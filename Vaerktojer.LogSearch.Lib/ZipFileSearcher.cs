using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using Vaerktojer.LogSearch.Data;

namespace Vaerktojer.LogSearch.Lib;

public sealed class ZipFileSearcher
{
    private static readonly Func<ZipArchiveEntry, bool> _defaultIncludeFileFilter = _ => true;
    private static readonly Func<ZipArchiveEntry, bool> _defaultExcludeFileFilter = _ => false;
    private const int _bufferSize = 1024;

    public static IEnumerable<SearchResult> SearchInZip<T>(
        string zipFilePath,
        T matcher,
        Func<T, string, bool> matcherFunc,
        Func<ZipArchiveEntry, bool>? includeFileFilter = null,
        Func<ZipArchiveEntry, bool>? excludeFileFilter = null,
        ZipFileSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        options ??= ZipFileSearchOptions.Default;
        includeFileFilter ??= _defaultIncludeFileFilter;
        excludeFileFilter ??= _defaultExcludeFileFilter;

        using var archive = ZipFile.OpenRead(zipFilePath);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                cancellationToken.ThrowIfCancellationRequested();

                lineNumber++;

                if (matcherFunc(matcher, line))
                {
                    yield return new(Path.Combine(zipFilePath, entry.FullName), lineNumber, line);

                    if (options.StopWhenFound)
                    {
                        yield break;
                    }
                }
            }
        }
    }

    public static async IAsyncEnumerable<SearchResult> SearchInZipAsync<T>(
        string zipFilePath,
        T matcher,
        Func<T, string, bool> matcherFunc,
        Func<ZipArchiveEntry, bool>? includeFileFilter = null,
        Func<ZipArchiveEntry, bool>? excludeFileFilter = null,
        ZipFileSearchOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        options ??= ZipFileSearchOptions.Default;
        includeFileFilter ??= _defaultIncludeFileFilter;
        excludeFileFilter ??= _defaultExcludeFileFilter;

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

            if (excludeFileFilter(entry) || !includeFileFilter(entry))
            {
                yield break;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            string? line;
            var lineNumber = 0;

            while (
                (line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null
            )
            {
                cancellationToken.ThrowIfCancellationRequested();

                lineNumber++;

                if (matcherFunc(matcher, line))
                {
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
