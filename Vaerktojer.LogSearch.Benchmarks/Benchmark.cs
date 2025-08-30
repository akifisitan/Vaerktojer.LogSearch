using System.IO.Compression;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Vaerktojer.LogSearch.Data;
using Vaerktojer.LogSearch.Lib;

namespace Vaerktojer.LogSearch.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class Benchmark
{
    private const string _basePath = @"C:\Users\user\Desktop\zipdemo";
    private const string _searchPattern = "hello123";
    private readonly Regex _regexSearchPattern = new(_searchPattern, RegexOptions.Compiled);
    private readonly DateTime _endTime = DateTime.Now;

    private static bool Matcher(string searchPattern, string content) =>
        content.Contains(searchPattern);

    private static bool RegexMatcher(string searchPattern, string content) =>
        content.Contains(searchPattern);

    [Benchmark(Baseline = true)]
    public void Bench1()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            Utils.IsZip,
            cancellationToken: cancellationToken
        );

        var options = new ZipFileSearchOptions(StopWhenFound: true);

        bool FileMatcher(ZipArchiveEntry entry) => entry.LastWriteTime < _endTime;

        foreach (var filePath in fileEnumerator)
        {
            var enumerator = ZipFileSearcher.SearchInZip(
                filePath,
                _searchPattern,
                Matcher,
                FileMatcher,
                options: options,
                cancellationToken: cancellationToken
            );

            foreach (var item in enumerator)
            {
                Console.WriteLine(item);
            }
        }
    }

    [Benchmark]
    public void Bench2()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            Utils.IsZip,
            cancellationToken: cancellationToken
        );

        var options = new ZipFileSearchOptions(StopWhenFound: true);
        var lineMatcher = new ContainsLineMatcher(_searchPattern);
        var fileMatcher = new ZipArchiveEntryMatcher(_endTime);

        foreach (var filePath in fileEnumerator)
        {
            var enumerator = ZipFileSearcher.SearchInZip(
                filePath,
                lineMatcher,
                fileMatcher,
                options: options,
                cancellationToken: cancellationToken
            );

            foreach (var item in enumerator)
            {
                Console.WriteLine(item);
            }
        }
    }

    [Benchmark]
    public async Task BenchAsync()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            _basePath,
            Utils.IsZip,
            cancellationToken: cancellationToken
        );

        var options = new ZipFileSearchOptions(StopWhenFound: true);

        foreach (var chunks in fileEnumerator.Chunk(5))
        {
            await Task.WhenAll(chunks.Select(Idk));
        }

        async Task Idk(string filePath)
        {
            var enumerator = ZipFileSearcher.SearchInZipAsync(
                filePath,
                _searchPattern,
                Matcher,
                entry => entry.LastWriteTime < DateTime.Now,
                options: options,
                cancellationToken: cancellationToken
            );

            await foreach (var item in enumerator)
            {
                Console.WriteLine(item);
            }
        }
    }
}
