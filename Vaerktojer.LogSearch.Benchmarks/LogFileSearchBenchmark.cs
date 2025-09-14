using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Vaerktojer.LogSearch.Lib.Core;
using Vaerktojer.LogSearch.Lib.Data;
using Vaerktojer.LogSearch.Lib.Filters;
using Vaerktojer.LogSearch.Lib.Matchers;

namespace Vaerktojer.LogSearch.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class LogFileSearchBenchmark
{
    private const string _basePath = @"D:\Temporary\zipdemo\logfiles";
    private const string _searchPattern = "f47de3ba-c88c-6c50-cae7-4b72b2e9346b";
    private readonly DateTimeOffset _startTime = DateTimeOffset.Now.Subtract(
        TimeSpan.FromDays(365)
    );
    private readonly DateTimeOffset _endTime = DateTimeOffset.Now;
    private const int _maxDegreeOfParallelism = 16;

    [Benchmark]
    public async Task Bench_Search_For_Pattern_In_Log_Files_In_Directory_By_Chunking_Async()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            new LogFileEnumerationFilter(_startTime, _endTime),
            cancellationToken: cancellationToken
        );

        var options = new FileSearchOptions(StopWhenFound: false);
        var lineMatcher = new StringContainsLineMatcher(_searchPattern);

        foreach (var chunks in fileEnumerator.Chunk(_maxDegreeOfParallelism))
        {
            await Task.WhenAll(chunks.Select(InternalSearch));
        }

        async Task InternalSearch(string filePath)
        {
            try
            {
                var enumerator = FileSearcher.SearchInFileAsync(
                    filePath,
                    lineMatcher,
                    options,
                    cancellationToken: cancellationToken
                );

                await foreach (
                    var item in enumerator.ConfigureAwait(false).WithCancellation(cancellationToken)
                )
                {
                    //Console.WriteLine(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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
            new LogFileEnumerationFilter(_startTime, _endTime),
            cancellationToken: cancellationToken
        );

        var options = new FileSearchOptions(StopWhenFound: false);
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
            catch (OperationCanceledException) { }
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
                        await InternalSearch(filePath, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        async Task InternalSearch(string filePath, CancellationToken cancellationToken)
        {
            var enumerator = FileSearcher.SearchInFileAsync(
                filePath,
                lineMatcher,
                options,
                cancellationToken: cancellationToken
            );

            await foreach (
                var item in enumerator.ConfigureAwait(false).WithCancellation(cancellationToken)
            )
            {
                //Console.WriteLine(item);
            }
        }
    }
}
