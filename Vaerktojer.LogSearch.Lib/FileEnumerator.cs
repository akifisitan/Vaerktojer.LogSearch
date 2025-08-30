namespace Vaerktojer.LogSearch.Lib;

public static class FileEnumerator
{
    private static readonly Func<string, bool> _defaultIncludeFilePredicate = _ => true;
    private static readonly Func<string, bool> _defaultIgnoreFilePredicate = _ => false;
    private static readonly EnumerationOptions _defaultEnumerationOptions = new()
    {
        IgnoreInaccessible = true,
    };

    public static IEnumerable<string> EnumerateFiles(
        string rootPath,
        Func<string, bool>? includeFilePredicate = null,
        Func<string, bool>? ignoreDirPredicate = null,
        EnumerationOptions? enumerationOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        includeFilePredicate ??= _defaultIncludeFilePredicate;
        ignoreDirPredicate ??= _defaultIgnoreFilePredicate;
        enumerationOptions ??= _defaultEnumerationOptions;

        var directoryStack = new Stack<string>();
        directoryStack.Push(rootPath);

        while (directoryStack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentPath = directoryStack.Pop();

            foreach (var filePath in Directory.EnumerateFiles(currentPath, "*", enumerationOptions))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (includeFilePredicate(filePath))
                {
                    yield return filePath;
                }
            }

            foreach (
                var dirPath in Directory.EnumerateDirectories(currentPath, "*", enumerationOptions)
            )
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ignoreDirPredicate(dirPath))
                {
                    directoryStack.Push(dirPath);
                }
            }
        }
    }
}
