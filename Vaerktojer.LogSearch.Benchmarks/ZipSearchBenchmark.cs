using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Vaerktojer.LogSearch.Data;
using Vaerktojer.LogSearch.Lib;

namespace Vaerktojer.LogSearch.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class ZipSearchBenchmark
{
    private const string _basePath = @"C:\Users\user\Desktop\zipdemo\multi";
    private const string _searchPattern = "f47de3ba-c88c-6c50-cae7-4b72b2e9346b";
    private readonly DateTimeOffset _startTime = DateTimeOffset.Now;
    private readonly DateTimeOffset _endTime = DateTimeOffset.Now;
    private const int _maxDegreeOfParallelism = 4;

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

        var options = new ZipFileSearchOptions(StopWhenFound: false);
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

        var options = new ZipFileSearchOptions(StopWhenFound: false);
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
    public async Task Bench_Search_For_Pattern_In_Log_Files_In_Directory_With_Channels()
    {
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            new ZipFileEnumerationFilter(),
            cancellationToken: cancellationToken
        );

        var options = new ZipFileSearchOptions(StopWhenFound: false);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);
        var fileMatcher = new ZipArchiveEntryFilter(_endTime);

        var capacity = Math.Max(_maxDegreeOfParallelism * 2, 1);
        var channel = Channel.CreateBounded<string>(
            new BoundedChannelOptions(capacity)
            {
                SingleWriter = true,
                SingleReader = _maxDegreeOfParallelism == 1,
                FullMode = BoundedChannelFullMode.Wait,
            }
        );

        var producerTask = Produce();

        var consumerTasks = Enumerable
            .Range(0, _maxDegreeOfParallelism)
            .Select(_ => Consume())
            .ToArray();

        try
        {
            await Task.WhenAll(consumerTasks.Prepend(producerTask));
        }
        catch
        {
            cts.Cancel();
            channel.Writer.TryComplete();
            throw;
        }

        async Task Produce()
        {
            try
            {
                foreach (var filePath in fileEnumerator)
                {
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

        async Task Consume()
        {
            try
            {
                while (await channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (channel.Reader.TryRead(out var filePath))
                    {
                        await InternalSearchInZipAsync(filePath, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // ignore
            }
        }

        async Task InternalSearchInZipAsync(string filePath, CancellationToken cancellationToken)
        {
            var enumerator = ZipFileSearcher.SearchInZipAsync(
                filePath,
                lineMatcher,
                fileMatcher,
                options,
                cancellationToken: cancellationToken
            );

            await foreach (
                var item in enumerator.ConfigureAwait(false).WithCancellation(cancellationToken)
            )
            {
                Console.WriteLine(item);
            }
        }
    }
}
