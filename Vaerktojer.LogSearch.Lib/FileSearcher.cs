using System.Runtime.CompilerServices;
using Vaerktojer.LogSearch.Abstractions;
using Vaerktojer.LogSearch.Data;

namespace Vaerktojer.LogSearch.Lib;

public sealed class FileSearcher
{
    private const int _bufferSize = 4096 * 2;

    public static IEnumerable<SearchResult> SearchInFile<TMatcher>(
        string filePath,
        TMatcher matcher,
        FileSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
        where TMatcher : ILineMatcher
    {
        options ??= FileSearchOptions.Default;

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _bufferSize,
            FileOptions.SequentialScan
        );

        using var reader = new StreamReader(stream);

        string? line;
        var lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lineNumber++;

            if (matcher.Match(line))
            {
                yield return new(filePath, lineNumber, line);

                if (options.StopWhenFound)
                {
                    yield break;
                }
            }
        }
    }

    public static async IAsyncEnumerable<SearchResult> SearchInFileAsync<TMatcher>(
        string filePath,
        TMatcher matcher,
        FileSearchOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TMatcher : ILineMatcher
    {
        options ??= FileSearchOptions.Default;

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        using var reader = new StreamReader(stream);

        string? line;
        var lineNumber = 0;

        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            lineNumber++;

            if (matcher.Match(line))
            {
                yield return new(filePath, lineNumber, line);

                if (options.StopWhenFound)
                {
                    yield break;
                }
            }
        }
    }
}
