using System.Buffers;
using System.Runtime.CompilerServices;
using Vaerktojer.LogSearch.Lib.Abstractions;
using Vaerktojer.LogSearch.Lib.Data;

namespace Vaerktojer.LogSearch.Lib.Core;

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

            if (matcher.IsMatch(line))
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

            if (matcher.IsMatch(line))
            {
                yield return new(filePath, lineNumber, line);

                if (options.StopWhenFound)
                {
                    yield break;
                }
            }
        }
    }

    public static async IAsyncEnumerable<SearchResult> SearchInFileFastAsync<TMatcher>(
        string filePath,
        TMatcher matcher,
        FileSearchOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
        where TMatcher : ISpanLineMatcher
    {
        options ??= FileSearchOptions.Default;

        // Tune buffer size as appropriate for your workload (e.g., 32K or 64K).
        const int BufferSize = 64 * 1024;

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        using var reader = new StreamReader(
            stream,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: BufferSize
        );

        var pool = ArrayPool<char>.Shared;
        char[] buffer = pool.Rent(BufferSize);

        // Accumulator used only when a line spans multiple buffer reads.
        ArrayBufferWriter<char>? acc = null;

        try
        {
            int lineNumber = 0;
            int charsRead = 0;
            int pos = 0; // current scan position within buffer
            int start = 0; // start of current (partial) line within buffer
            bool havePartial = false;

            while (true)
            {
                // Refill if we consumed the buffer
                if (pos >= charsRead)
                {
                    charsRead = await reader
                        .ReadAsync(buffer.AsMemory(), cancellationToken)
                        .ConfigureAwait(false);
                    pos = 0;
                    start = 0;

                    if (charsRead == 0)
                    {
                        // EOF: emit the last line if we have a partial one without trailing newline
                        if (havePartial)
                        {
                            lineNumber++;
                            var lineSpan = acc is null ? [] : acc.WrittenSpan;

                            if (matcher.IsMatch(lineSpan))
                            {
                                var lineStr = new string(lineSpan);
                                yield return new(filePath, lineNumber, lineStr);

                                if (options.StopWhenFound)
                                {
                                    yield break;
                                }
                            }
                        }
                        yield break;
                    }
                }

                // Find next newline in buffer
                int idx = buffer.AsSpan(pos, charsRead - pos).IndexOf('\n');
                if (idx >= 0)
                {
                    int end = pos + idx; // index of '\n'
                    int len = end - start; // length up to '\n' (may include '\r' at end)
                    int effectiveLen = len > 0 && buffer[end - 1] == '\r' ? len - 1 : len;

                    ReadOnlySpan<char> lineSpan;
                    if (acc is null)
                    {
                        // Entire line is within current buffer
                        lineSpan = buffer.AsSpan(start, effectiveLen);
                    }
                    else
                    {
                        // Line spans multiple reads, append final piece
                        acc.Write(buffer.AsSpan(start, effectiveLen));
                        lineSpan = acc.WrittenSpan;
                    }

                    lineNumber++;

                    if (matcher.IsMatch(lineSpan))
                    {
                        // Only allocate a string when matched
                        var lineStr = new string(lineSpan);
                        yield return new(filePath, lineNumber, lineStr);

                        if (options.StopWhenFound)
                        {
                            yield break;
                        }
                    }

                    // Reset for next line
                    acc = null;
                    havePartial = false;

                    pos = end + 1; // move past '\n'
                    start = pos;
                }
                else
                {
                    // No newline found: stash remaining segment and refill
                    var remaining = buffer.AsSpan(start, charsRead - start);
                    if (!remaining.IsEmpty)
                    {
                        acc ??= new ArrayBufferWriter<char>(Math.Min(remaining.Length * 2, 4096));
                        acc.Write(remaining);
                        havePartial = true;
                    }

                    // Consume buffer and force a refill on next loop
                    pos = charsRead;
                    start = pos;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }
}
