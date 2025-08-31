using System.IO.Enumeration;

namespace Vaerktojer.LogSearch.Abstractions;

public interface IIncludeable<T>
{
    bool Include(T value);
}

public interface IFileSystemEnumerationFilter
{
    bool IncludeFile(ref FileSystemEntry entry);
    bool ExcludeDirectory(ref FileSystemEntry entry);
}
