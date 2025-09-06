using System.IO.Enumeration;
using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Core;

public static class FileEnumerator
{
    public static IEnumerable<string> EnumerateFiles<TFilter>(
        string rootDirectoryPath,
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
                FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
            ReturnSpecialDirectories = false,
        };

        var enumeration = new FileSystemEnumerable<string>(
            directory: rootDirectoryPath,
            transform: (ref FileSystemEntry entry) => entry.ToFullPath(),
            options: enumerationOptions
        )
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDirectory)
                {
                    return false;
                }

                return filter.IncludeFile(ref entry);
            },
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return !filter.ExcludeDirectory(ref entry);
            },
        };

        foreach (var path in enumeration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return path;
        }
    }

    public static IEnumerable<FileSystemInfo> EnumerateFilesToFileSystemInfo<TFilter>(
        string rootDirectoryPath,
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
                FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
            ReturnSpecialDirectories = false,
        };

        var enumeration = new FileSystemEnumerable<FileSystemInfo>(
            directory: rootDirectoryPath,
            transform: (ref FileSystemEntry entry) => entry.ToFileSystemInfo(),
            options: enumerationOptions
        )
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDirectory)
                {
                    return false;
                }

                return filter.IncludeFile(ref entry);
            },
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

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
