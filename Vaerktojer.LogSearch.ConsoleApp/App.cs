using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Vaerktojer.LogSearch.Lib.Core;
using Vaerktojer.LogSearch.Lib.Data;
using Vaerktojer.LogSearch.Lib.Filters;
using Vaerktojer.LogSearch.Lib.Matchers;
using ZLogger;

namespace Vaerktojer.LogSearch.ConsoleApp;

internal class App(ILogger<App> logger)
{
    private const string _basePath = @"C:\Users\user";
    private const string _searchPattern = "f47de3ba-c88c-6c50-cae7-4b72b2e9346b";
    private readonly DateTimeOffset _startTime = DateTimeOffset.Now;
    private readonly DateTimeOffset _endTime = DateTimeOffset.Now;
    private const int _maxDegreeOfParallelism = 4;

    public async Task Run()
    {
        var r = new Random();

        while (true)
        {
            var randomValue = r.Next(100);

            logger.ZLogInformation($"Hello World from my method. Random value: {randomValue}");

            if (randomValue == 1)
            {
                break;
            }

            await Task.Delay(500);
        }

        throw new Exception("wowzer");

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator
            .EnumerateFiles(
                _basePath,
                new TextFileEnumerationFilter(),
                cancellationToken: cancellationToken
            )
            .ToList();

        var v = Prompt.Prompt.Select(
            "Select file",
            fileEnumerator,
            pageSize: 20,
            textSelector: x => x[..Math.Min(50, x.Length)]
        );
    }

    private async Task Idk()
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
