using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Vaerktojer.LogSearch.Data;
using Vaerktojer.LogSearch.Lib;

namespace Vaerktojer.LogSearch.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public sealed class Benchmark
{
    private const string basePath = @"C:\";
    private const string searchPattern = "";
    private readonly Regex regexSearchPattern = new(searchPattern, RegexOptions.Compiled);

    private static bool Matcher(string searchPattern, string content) =>
        content.Contains(searchPattern);

    private static bool RegexMatcher(string searchPattern, string content) =>
        content.Contains(searchPattern);

    private static bool FileIsZip(string path) =>
        string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);

    [Benchmark(Baseline = true)]
    public void Bench1()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            basePath,
            FileIsZip,
            cancellationToken: cancellationToken
        );

        var options = new ZipFileSearchOptions(StopWhenFound: true);

        foreach (var filePath in fileEnumerator)
        {
            var enumerator = ZipFileSearcher.SearchInZip(
                filePath,
                searchPattern,
                Matcher,
                entry => entry.LastWriteTime < DateTime.Now,
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
    public async Task Bench2()
    {
        using var cts = new CancellationTokenSource();

        var cancellationToken = cts.Token;

        var fileEnumerator = FileEnumerator.EnumerateFiles(
            basePath,
            FileIsZip,
            cancellationToken: cancellationToken
        );

        var options = new ZipFileSearchOptions(StopWhenFound: true);

        foreach (var filePath in fileEnumerator)
        {
            var enumerator = ZipFileSearcher.SearchInZipAsync(
                filePath,
                searchPattern,
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
