using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Vaerktojer.LogSearch.Lib.Core;
using Vaerktojer.LogSearch.Lib.Data;
using Vaerktojer.LogSearch.Lib.Filters;
using Vaerktojer.LogSearch.Lib.Matchers;

namespace Vaerktojer.LogSearch.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class Benchmark
{
    private const string _basePath = @"C:\Users\user\Desktop\zipdemo";
    private const string _searchPattern = "hello123";
    private readonly DateTimeOffset _startTime = DateTimeOffset.Now;
    private readonly DateTimeOffset _endTime = DateTimeOffset.Now;
    private const int _maxDegreeOfParallelism = 8;

    //[Benchmark(Baseline = true)]
    public void Bench_Search_For_Pattern_In_Zip_Files_In_Directory_Sequential()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            new ZipFileEnumerationFilter(),
            cancellationToken: cancellationToken
        );

        var options = new ZipFileSearchOptions(StopWhenFound: true);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);
        var zipArchiveEntryFilter = new ZipArchiveEntryFilter(_endTime);

        foreach (var filePath in fileEnumerator)
        {
            var enumerator = ZipFileSearcher.SearchInZip(
                filePath,
                lineMatcher,
                zipArchiveEntryFilter,
                options,
                cancellationToken: cancellationToken
            );

            foreach (var item in enumerator)
            {
                Console.WriteLine(item);
            }
        }
    }

    [Benchmark]
    public async Task Bench_Search_For_Pattern_In_Zip_Files_In_Directory_By_Chunking_Async()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            new ZipFileEnumerationFilter(),
            cancellationToken: cancellationToken
        );

        var options = new ZipFileSearchOptions(StopWhenFound: true);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);
        var fileMatcher = new ZipArchiveEntryFilter(_endTime);

        foreach (var chunks in fileEnumerator.Chunk(_maxDegreeOfParallelism))
        {
            await Task.WhenAll(chunks.Select(InternalSearchInZipAsync));
        }

        async Task InternalSearchInZipAsync(string filePath)
        {
            var enumerator = ZipFileSearcher.SearchInZipAsync(
                filePath,
                lineMatcher,
                fileMatcher,
                options,
                cancellationToken: cancellationToken
            );

            await foreach (var item in enumerator)
            {
                Console.WriteLine(item);
            }
        }
    }

    [Benchmark]
    public void Bench_Search_For_Pattern_In_Log_Files_In_Directory_Sequential()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            new LogFileEnumerationFilter(_startTime, _endTime),
            cancellationToken: cancellationToken
        );

        var options = new FileSearchOptions(StopWhenFound: true);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);

        foreach (var filePath in fileEnumerator)
        {
            var enumerator = FileSearcher.SearchInFile(
                filePath,
                lineMatcher,
                options,
                cancellationToken: cancellationToken
            );

            foreach (var item in enumerator)
            {
                Console.WriteLine(item);
            }
        }
    }

    [Benchmark]
    public async Task Bench_Search_For_Pattern_In_Log_Files_In_Directory_Async()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            new LogFileEnumerationFilter(_startTime, _endTime),
            cancellationToken: cancellationToken
        );

        var options = new FileSearchOptions(StopWhenFound: true);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);

        foreach (var chunks in fileEnumerator.Chunk(_maxDegreeOfParallelism))
        {
            await Task.WhenAll(chunks.Select(InternalSearchInZipAsync));
        }

        async Task InternalSearchInZipAsync(string filePath)
        {
            var enumerator = FileSearcher.SearchInFileAsync(
                filePath,
                lineMatcher,
                options,
                cancellationToken: cancellationToken
            );

            await foreach (var item in enumerator)
            {
                Console.WriteLine(item);
            }
        }
    }

    [Benchmark]
    public async Task Bench_Search_For_Pattern_In_Log_Files_In_Directory_With_Channels_Gpt5_T3_Async()
    {
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var options = new FileSearchOptions(StopWhenFound: true);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);

        var capacity = Math.Max(_maxDegreeOfParallelism * 2, 1);
        var channel = Channel.CreateBounded<string>(
            new BoundedChannelOptions(capacity)
            {
                SingleWriter = true,
                SingleReader = _maxDegreeOfParallelism == 1,
                FullMode = BoundedChannelFullMode.Wait,
            }
        );

        // Start consumers first so production can immediately feed them.
        var consumers = Enumerable
            .Range(0, _maxDegreeOfParallelism)
            .Select(_ => ConsumeAsync(channel.Reader, cancellationToken))
            .ToArray();

        // Produce file paths into the bounded channel (no Task.Run needed).
        Exception? producerError = null;
        try
        {
            var fileEnumerator = FileEnumerator.EnumerateFiles(
                _basePath,
                new LogFileEnumerationFilter(_startTime, _endTime),
                cancellationToken: cancellationToken
            );

            foreach (var filePath in fileEnumerator)
            {
                // Backpressure: waits if buffer is full.
                await channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false);

                if (!channel.Writer.TryWrite(filePath))
                {
                    break; // Completed or canceled.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Swallow expected cancellation.
        }
        catch (Exception ex)
        {
            producerError = ex;
            cts.Cancel();
        }
        finally
        {
            channel.Writer.TryComplete(producerError);
        }

        // Await all consumers (propagates any failures).
        await Task.WhenAll(consumers).ConfigureAwait(false);

        async Task ConsumeAsync(ChannelReader<string> reader, CancellationToken ct)
        {
            try
            {
                await foreach (var filePath in reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    await InternalSearchInFileAsync(filePath, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Graceful shutdown.
            }
            catch
            {
                // Fail fast: cancel siblings and rethrow to surface the error.
                cts.Cancel();
                throw;
            }
        }

        async Task InternalSearchInFileAsync(string filePath, CancellationToken ct)
        {
            var enumerator = FileSearcher.SearchInFileAsync(
                filePath,
                lineMatcher,
                options,
                cancellationToken: ct
            );

            await foreach (var item in enumerator.ConfigureAwait(false))
            {
                Console.WriteLine(item);
            }
        }
    }

    [Benchmark]
    public async Task Bench_Search_For_Pattern_In_Log_Files_In_Directory_With_Channels_Gpt5_Uncovr_Async()
    {
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            new LogFileEnumerationFilter(_startTime, _endTime),
            cancellationToken: cancellationToken
        );

        var options = new FileSearchOptions(StopWhenFound: true);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);

        // Bounded channel to decouple production from consumption and add backpressure
        var capacity = _maxDegreeOfParallelism * 2; // small buffer helps smooth spikes
        var channel = Channel.CreateBounded<string>(
            new BoundedChannelOptions(capacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait,
            }
        );

        async Task ProducerAsync()
        {
            try
            {
                foreach (var filePath in fileEnumerator)
                {
                    // Backpressure is applied here if the channel is full
                    await channel.Writer.WriteAsync(filePath, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // ignore
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }

        async Task ConsumerAsync()
        {
            try
            {
                // Drain the channel
                while (await channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (channel.Reader.TryRead(out var filePath))
                    {
                        await InternalSearchInFileAsync(filePath, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // ignore
            }
        }

        async Task InternalSearchInFileAsync(string filePath, CancellationToken ct)
        {
            var results = FileSearcher.SearchInFileAsync(
                filePath,
                lineMatcher,
                options,
                cancellationToken: ct
            );

            await foreach (var item in results.WithCancellation(ct))
            {
                Console.WriteLine(item);
            }
        }

        // Start producer and a fixed number of consumers (no Task.Run per file)
        var producerTask = ProducerAsync(); // starts immediately (no extra thread)
        var consumerTasks = Enumerable
            .Range(0, _maxDegreeOfParallelism)
            .Select(_ => ConsumerAsync())
            .ToArray();

        try
        {
            await Task.WhenAll(consumerTasks.Prepend(producerTask));
        }
        catch
        {
            // If anything goes wrong, cancel and ensure channel is completed
            cts.Cancel();
            channel.Writer.TryComplete();
            throw;
        }
    }

    [Benchmark]
    public async Task Bench_Search_For_Pattern_In_Log_Files_In_Directory_With_Channels_Async()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            new LogFileEnumerationFilter(_startTime, _endTime),
            cancellationToken: cancellationToken
        );

        var options = new FileSearchOptions(StopWhenFound: true);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);

        var channel = Channel.CreateBounded<string>(
            new BoundedChannelOptions(_maxDegreeOfParallelism * 2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = false,
            }
        );

        var writer = channel.Writer;
        var reader = channel.Reader;

        async Task Produce()
        {
            foreach (var filePath in fileEnumerator)
            {
                await writer.WriteAsync(filePath);
            }

            writer.Complete();
        }

        async Task Consume()
        {
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var result))
                {
                    await InternalSearchInZipAsync(result);
                }
            }
        }

        var consumerTasks = Enumerable
            .Range(0, _maxDegreeOfParallelism)
            .Select(_ => Consume())
            .ToList();

        var producerTask = Produce();

        await Task.WhenAll(consumerTasks.Prepend(producerTask));

        async Task InternalSearchInZipAsync(string filePath)
        {
            var enumerator = FileSearcher.SearchInFileAsync(
                filePath,
                lineMatcher,
                options,
                cancellationToken: cancellationToken
            );

            await foreach (var item in enumerator)
            {
                Console.WriteLine(item);
            }
        }
    }
}
