using BenchmarkDotNet.Attributes;
using Vaerktojer.LogSearch.Data;
using Vaerktojer.LogSearch.Lib;

namespace Vaerktojer.LogSearch.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class Benchmark
{
    private const string _basePath = @"C:\Users\user\Desktop\zipdemo\single";
    private const string _searchPattern = "hello123";
    private readonly DateTimeOffset _startTime = DateTimeOffset.Now;
    private readonly DateTimeOffset _endTime = DateTimeOffset.Now;
    private const int _maxDegreeOfParallelism = 8;

    [Benchmark(Baseline = true)]
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
    public async Task Bench_Search_For_Pattern_In_Zip_Files_In_Directory_Async()
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
}
