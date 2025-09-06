//using System.IO.Compression;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Channels;
//using Vaerktojer.LogSearch.Data;

//namespace Vaerktojer.LogSearch.Lib;

//public sealed class DirectorySearcher
//{
//    public static async IAsyncEnumerable<SearchResult> SearchInZipAsync<T>(
//        string zipFilePath,
//        T matcher,
//        Func<T, string, bool> matcherFunc,
//        Func<ZipArchiveEntry, bool>? includeFileFilter = null,
//        ZipFileSearchOptions? options = null,
//        [EnumeratorCancellation] CancellationToken cancellationToken = default
//    )
//    {
//        options ??= ZipFileSearchOptions.Default;
//        includeFileFilter ??= static a => true;

//        // Tune these to your needs or surface via options if desired.
//        var maxDegree = Math.Max(1, Math.Min(Environment.ProcessorCount, 8));
//        var workQueueCapacity = maxDegree * 4; // bounded work-queue (entry names)
//        var resultQueueCapacity = 256; // bounded results channel

//        var work = Channel.CreateBounded<string>(
//            new BoundedChannelOptions(workQueueCapacity)
//            {
//                FullMode = BoundedChannelFullMode.Wait,
//                SingleWriter = true,
//                SingleReader = false,
//            }
//        );

//        var results = Channel.CreateBounded<SearchResult>(
//            new BoundedChannelOptions(resultQueueCapacity)
//            {
//                FullMode = BoundedChannelFullMode.Wait,
//                SingleWriter = false,
//                SingleReader = true,
//            }
//        );

//        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//        var ct = linkedCts.Token;
//        int stopFlag = 0;

//        // Producer: enumerate the archive once, push eligible entry names into the work channel.
//        var producer = Task.Run(
//            async () =>
//            {
//                Exception? ex = null;

//                try
//                {
//                    await using var zipFs = new FileStream(
//                        zipFilePath,
//                        FileMode.Open,
//                        FileAccess.Read,
//                        FileShare.Read,
//                        _bufferSize,
//                        FileOptions.Asynchronous | FileOptions.SequentialScan
//                    );

//                    using var archive = new ZipArchive(
//                        zipFs,
//                        ZipArchiveMode.Read,
//                        leaveOpen: false,
//                        Encoding.UTF8
//                    );

//                    foreach (var entry in archive.Entries)
//                    {
//                        ct.ThrowIfCancellationRequested();

//                        if (!includeFileFilter(entry))
//                        {
//                            // Skip excluded entries rather than stopping the whole search.
//                            continue;
//                        }

//                        await work.Writer.WriteAsync(entry.FullName, ct).ConfigureAwait(false);
//                    }
//                }
//                catch (OperationCanceledException) when (ct.IsCancellationRequested)
//                {
//                    // Normal cancellation path
//                }
//                catch (Exception e)
//                {
//                    ex = e;
//                }
//                finally
//                {
//                    // Completing the work channel will stop workers.
//                    if (ex != null)
//                        work.Writer.TryComplete(ex);
//                    else
//                        work.Writer.TryComplete();
//                }
//            },
//            CancellationToken.None
//        );

//        // Worker function: open the zip per task, find the entry by name, and scan for matches.
//        async Task WorkerAsync()
//        {
//            await foreach (var entryName in work.Reader.ReadAllAsync(ct).ConfigureAwait(false))
//            {
//                if (ct.IsCancellationRequested)
//                    break;

//                try
//                {
//                    await using var fs = new FileStream(
//                        zipFilePath,
//                        FileMode.Open,
//                        FileAccess.Read,
//                        FileShare.Read,
//                        _bufferSize,
//                        FileOptions.Asynchronous | FileOptions.SequentialScan
//                    );

//                    using var arc = new ZipArchive(
//                        fs,
//                        ZipArchiveMode.Read,
//                        leaveOpen: false,
//                        Encoding.UTF8
//                    );

//                    var entry = arc.GetEntry(entryName);
//                    if (entry is null)
//                    {
//                        // Entry might not be found; skip safely.
//                        continue;
//                    }

//                    using var stream = entry.Open();
//                    using var reader = new StreamReader(stream);

//                    string? line;
//                    var lineNumber = 0;

//                    while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
//                    {
//                        lineNumber++;
//                        if (matcherFunc(matcher, line))
//                        {
//                            var result = new SearchResult(
//                                Path.Combine(zipFilePath, entry.FullName),
//                                lineNumber,
//                                line
//                            );

//                            // This awaits if the results channel is full (bounded).
//                            await results.Writer.WriteAsync(result, ct).ConfigureAwait(false);

//                            if (
//                                options.StopWhenFound
//                                && Interlocked.CompareExchange(ref stopFlag, 1, 0) == 0
//                            )
//                            {
//                                // First result found: cancel all remaining work.
//                                linkedCts.Cancel();
//                                break;
//                            }
//                        }
//                    }
//                }
//                catch (OperationCanceledException) when (ct.IsCancellationRequested)
//                {
//                    // Normal cancellation path
//                    break;
//                }
//            }
//        }

//        // Start worker pool
//        var workers = new List<Task>(maxDegree);
//        for (int i = 0; i < maxDegree; i++)
//        {
//            workers.Add(Task.Run(WorkerAsync, CancellationToken.None));
//        }

//        // Complete the results channel when producer and workers are done.
//        var completeResults = Task.Run(
//            async () =>
//            {
//                Exception? ex = null;
//                try
//                {
//                    await producer.ConfigureAwait(false);
//                    await Task.WhenAll(workers).ConfigureAwait(false);
//                }
//                catch (Exception e)
//                {
//                    ex = e;
//                    linkedCts.Cancel();
//                }
//                finally
//                {
//                    if (ex != null)
//                        results.Writer.TryComplete(ex);
//                    else
//                        results.Writer.TryComplete();
//                }
//            },
//            CancellationToken.None
//        );

//        // Stream results to the caller as they arrive.
//        await foreach (
//            var item in results.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)
//        )
//        {
//            yield return item;
//        }

//        // Ensure completion and propagate exceptions after enumeration finishes.
//        await completeResults.ConfigureAwait(false);
//    }
//}
