using System.IO.Enumeration;
using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Filters;

public readonly record struct TextFileEnumerationFilter : IFileSystemEnumerationFilter
{
    public bool IncludeFile(ref FileSystemEntry entry) =>
        Path.GetExtension(entry.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase);

    public bool ExcludeDirectory(ref FileSystemEntry entry) => false;
}
