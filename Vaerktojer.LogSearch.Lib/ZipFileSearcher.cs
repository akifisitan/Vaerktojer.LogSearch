using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using Vaerktojer.LogSearch.Abstractions;
using Vaerktojer.LogSearch.Data;

namespace Vaerktojer.LogSearch.Lib;

public sealed class ZipFileSearcher
{
    private const int _bufferSize = 4096;

    public static IEnumerable<SearchResult> SearchInZip<TLineMatcher, TZipArchiveEntryFilter>(
        string zipFilePath,
        TLineMatcher lineMatcher,
        TZipArchiveEntryFilter zipArchiveEntryFilter,
        ZipFileSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
        where TLineMatcher : ILineMatcher
        where TZipArchiveEntryFilter : IZipArchiveEntryFilter
    {
        options ??= ZipFileSearchOptions.Default;

        using var archive = ZipFile.OpenRead(zipFilePath);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!zipArchiveEntryFilter.Include(entry))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            string? line;
            var lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lineNumber++;

                if (lineMatcher.IsMatch(line))
                {
                    yield return new(Path.Combine(zipFilePath, entry.FullName), lineNumber, line);

                    if (options.StopWhenFound)
                    {
                        break;
                    }
                }
            }
        }
    }

    public static async IAsyncEnumerable<SearchResult> SearchInZipAsync<
        TLineMatcher,
        TZipArchiveEntryFilter
    >(
        string zipFilePath,
        TLineMatcher lineMatcher,
        TZipArchiveEntryFilter zipArchiveEntryFilter,
        ZipFileSearchOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TLineMatcher : ILineMatcher
        where TZipArchiveEntryFilter : IZipArchiveEntryFilter
    {
        options ??= ZipFileSearchOptions.Default;

        await using var zipFs = new FileStream(
            zipFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        cancellationToken.ThrowIfCancellationRequested();

        using var archive = new ZipArchive(
            zipFs,
            ZipArchiveMode.Read,
            leaveOpen: false,
            Encoding.UTF8
        );

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!zipArchiveEntryFilter.Include(entry))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            string? line;
            var lineNumber = 0;

            while (
                (line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null
            )
            {
                lineNumber++;

                if (lineMatcher.IsMatch(line))
                {
                    yield return new(Path.Combine(zipFilePath, entry.FullName), lineNumber, line);

                    if (options.StopWhenFound)
                    {
                        break;
                    }
                }
            }
        }
    }
}
