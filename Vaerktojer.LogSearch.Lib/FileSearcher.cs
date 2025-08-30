using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Vaerktojer.LogSearch.Data;

namespace Vaerktojer.LogSearch.Lib;

public sealed class FileSearcher
{
    private const int bufferSize = 4096 * 2;

    public static void Search(
        string searchPattern,
        string searchDirectory,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        bool searchZip = false,
        string includePattern = "*",
        string? excludePattern = null,
        FileSearchOptions? fileSearchOptions = null,
        ZipFileSearchOptions? zipFileSearchOptions = null
    )
    {
        var searchRegex = new Regex(
            searchPattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(250)
        );

        var matcher = new Matcher().AddInclude(includePattern);

        if (!string.IsNullOrWhiteSpace(excludePattern))
        {
            matcher.AddExclude(excludePattern);
        }

        var filePathEnumerator = FileEnumerator.EnumerateFiles(
            searchDirectory,
            includeFilePredicate: path =>
            {
                if (!matcher.Match(searchDirectory, path).HasMatches)
                {
                    return false;
                }

                var lastWriteTime = File.GetLastWriteTime(path);

                //lastWriteTime.TimeOfDay >= startTime && lastWriteTime <= endTime

                //return lastWriteTime >= startDate && lastWriteTime <= endDate;

                return (startDate, endDate) switch
                {
                    (null, null) => true,
                    (not null, null) => startDate <= lastWriteTime,
                    (null, not null) => lastWriteTime <= endDate,
                    (not null, not null) => startDate <= lastWriteTime && lastWriteTime <= endDate,
                };
            }
        );

        var sw = Stopwatch.StartNew();

        var count = 0;

        foreach (var filePath in filePathEnumerator)
        {
            InternalSearch(filePath);
            count++;
        }

        Console.WriteLine(
            $"Finished searching through {count} files in {sw.Elapsed.TotalSeconds} seconds."
        );

        static bool IsZip(string path) =>
            Path.GetExtension(path)?.Equals(".zip", StringComparison.OrdinalIgnoreCase) is true;

        void InternalSearch(string path)
        {
            try
            {
                var searchFileResult =
                    !IsZip(path) ? SearchInFile(path, searchRegex, options: fileSearchOptions)
                    : searchZip
                        ? ZipFileSearcher.SearchInZip(
                            path,
                            searchRegex,
                            options: zipFileSearchOptions,
                            includeFileFilter: entry =>
                                matcher.Match(entry.FullName).HasMatches
                                && entry.LastWriteTime >= startDate
                                && entry.LastWriteTime <= endDate
                        )
                    : [];

                foreach (var searchResult in searchFileResult)
                {
                    Log($"{searchResult}");
                }
            }
            catch (FileNotFoundException)
            {
                Log($"Error: File not found at '{path}'");
            }
        }
    }

    private static IEnumerable<SearchResult> SearchInFile(
        string filePath,
        Regex searchRegex,
        FileSearchOptions? options = null
    )
    {
        options ??= FileSearchOptions.Default;

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            FileOptions.SequentialScan
        );

        using var reader = new StreamReader(stream);

        string? line;
        var lineNumber = 0;
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            if (searchRegex.IsMatch(line))
            {
                yield return new(filePath, lineNumber, line);
                if (options.StopWhenFound)
                {
                    yield break;
                }
            }
        }
    }

    private static async IAsyncEnumerable<SearchResult> SearchInFileAsync(
        string filePath,
        Regex searchRegex,
        FileSearchOptions? options = null
    )
    {
        options ??= FileSearchOptions.Default;

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        using var reader = new StreamReader(stream);

        string? line;
        var lineNumber = 0;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            lineNumber++;
            if (searchRegex.IsMatch(line))
            {
                yield return new(filePath, lineNumber, line);

                if (options.StopWhenFound)
                {
                    yield break;
                }
            }
        }
    }

    private static void Log(string message)
    {
        Console.WriteLine(message);
    }
}
