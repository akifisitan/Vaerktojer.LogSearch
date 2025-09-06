using System.IO.Enumeration;
using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Filters;

public readonly record struct ZipFileEnumerationFilter : IFileSystemEnumerationFilter
{
    public bool IncludeFile(ref FileSystemEntry entry) =>
        Path.GetExtension(entry.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    public bool ExcludeDirectory(ref FileSystemEntry entry) => false;
}
