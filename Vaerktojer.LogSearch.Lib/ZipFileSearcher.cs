using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Vaerktojer.LogSearch.Data;

namespace Vaerktojer.LogSearch.Lib;

public sealed class ZipFileSearcher
{
    public static IEnumerable<SearchResult> SearchInZip(
        string zipFilePath,
        Regex searchRegex,
        Func<ZipArchiveEntry, bool>? includeFileFilter = null,
        Func<ZipArchiveEntry, bool>? excludeFileFilter = null,
        ZipFileSearchOptions? options = null
    )
    {
        try
        {
            return SearchInZipInternal(
                zipFilePath,
                searchRegex,
                includeFileFilter,
                excludeFileFilter,
                options
            );
        }
        catch (InvalidDataException)
        {
            //Log($"Error: The file at '{zipFilePath}' is not a valid zip archive.");
            return [];
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            //Log($"An unexpected error occurred: {ex.Message}");
            return [];
        }
    }

    private static IEnumerable<SearchResult> SearchInZipInternal(
        string zipFilePath,
        Regex searchRegex,
        Func<ZipArchiveEntry, bool>? includeFileFilter = null,
        Func<ZipArchiveEntry, bool>? excludeFileFilter = null,
        ZipFileSearchOptions? options = null
    )
    {
        options ??= ZipFileSearchOptions.Default;
        includeFileFilter ??= _ => true;
        excludeFileFilter ??= _ => false;

        // Warning: This stream is not thread safe
        using var archive = ZipFile.OpenRead(zipFilePath);

        List<string> tp = [];

        foreach (var entry in archive.Entries)
        {
            if (excludeFileFilter(entry) || !includeFileFilter(entry))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            string? line;
            var lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                if (searchRegex.IsMatch(line))
                {
                    tp.Add(entry.FullName);

                    if (!string.IsNullOrWhiteSpace(options.ExtractPath))
                    {
                        var sw = Stopwatch.StartNew();
                        entry.ExtractToFile($"{entry.FullName.Replace('/', '-')}", overwrite: true);
                        Console.WriteLine(
                            $"Time taken to extract {entry.Length} bytes: {sw.Elapsed.TotalMilliseconds} ms."
                        );
                    }

                    yield return new(Path.Combine(zipFilePath, entry.FullName), lineNumber, line);

                    if (options.StopWhenFound)
                    {
                        yield break;
                    }
                }
            }
        }
    }
}
