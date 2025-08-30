using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using Vaerktojer.LogSearch.Abstractions;
using Vaerktojer.LogSearch.Data;

namespace Vaerktojer.LogSearch.Lib;

public sealed class ZipFileSearcher
{
    private static readonly Func<ZipArchiveEntry, bool> _defaultIncludeFileFilter = _ => true;
    private const int _bufferSize = 1024;

    public static IEnumerable<SearchResult> SearchInZip<TLineMatcher, TFileMatcher>(
        string zipFilePath,
        TLineMatcher matcher,
        TFileMatcher fileFilter,
        ZipFileSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
        where TLineMatcher : ILineMatcher
        where TFileMatcher : IIncludeable<ZipArchiveEntry>
    {
        options ??= ZipFileSearchOptions.Default;

        using var archive = ZipFile.OpenRead(zipFilePath);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!fileFilter.Include(entry))
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

                if (matcher.Match(line))
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

    public static IEnumerable<SearchResult> SearchInZip<TMatchable>(
        string zipFilePath,
        TMatchable matchable,
        Func<TMatchable, string, bool> matcher,
        Func<ZipArchiveEntry, bool>? includeFileFilter = null,
        ZipFileSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
        where TMatchable : notnull
    {
        options ??= ZipFileSearchOptions.Default;
        includeFileFilter ??= _defaultIncludeFileFilter;

        using var archive = ZipFile.OpenRead(zipFilePath);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!includeFileFilter(entry))
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

                if (matcher(matchable, line))
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

    public static async IAsyncEnumerable<SearchResult> SearchInZipAsyncSlow<T>(
        string zipFilePath,
        T matcher,
        Func<T, string, bool> matcherFunc,
        Func<ZipArchiveEntry, bool>? includeFileFilter = null,
        ZipFileSearchOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        options ??= ZipFileSearchOptions.Default;
        includeFileFilter ??= _defaultIncludeFileFilter;

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

            if (!includeFileFilter(entry))
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

    public static async IAsyncEnumerable<SearchResult> SearchInZipAsync<T>(
        string zipFilePath,
        T matcher,
        Func<T, string, bool> matcherFunc,
        Func<ZipArchiveEntry, bool>? includeFileFilter = null,
        ZipFileSearchOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        options ??= ZipFileSearchOptions.Default;
        includeFileFilter ??= _defaultIncludeFileFilter;

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

            if (!includeFileFilter(entry))
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
