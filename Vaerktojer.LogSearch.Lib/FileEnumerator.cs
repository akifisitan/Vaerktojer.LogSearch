using System.IO.Enumeration;
using Vaerktojer.LogSearch.Abstractions;

namespace Vaerktojer.LogSearch.Lib;

public static class FileEnumerator
{
    public static IEnumerable<string> EnumerateFiles<TFilter>(
        string rootPath,
        TFilter filter,
        CancellationToken cancellationToken = default
    )
        where TFilter : IFileSystemEnumerationFilter
    {
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip =
                FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System,
            ReturnSpecialDirectories = false,
        };

        var enumeration = new FileSystemEnumerable<string>(
            directory: rootPath,
            transform: (ref FileSystemEntry entry) => entry.ToFullPath(),
            options: enumerationOptions
        )
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!entry.IsDirectory)
                {
                    return filter.IncludeFile(ref entry);
                }
                return false;
            },
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return false;
                }

                return !filter.ExcludeDirectory(ref entry);
            },
        };

        foreach (var path in enumeration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return path;
        }
    }
}
