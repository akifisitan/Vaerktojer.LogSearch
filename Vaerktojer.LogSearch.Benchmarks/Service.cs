using System.IO.Compression;
using System.IO.Enumeration;
using Vaerktojer.LogSearch.Abstractions;

namespace Vaerktojer.LogSearch.Benchmarks;

public readonly record struct ContainsLineMatcher : ILineMatcher
{
    private readonly string _value;

    public ContainsLineMatcher(string value)
    {
        _value = value;
    }

    public bool Match(string line) => line.Contains(_value);
}

public sealed class ZipFileSystemEnumerationFilter : IFileSystemEnumerationFilter
{
    public bool IncludeFile(ref FileSystemEntry entry) =>
        Path.GetExtension(entry.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    public bool ExcludeDirectory(ref FileSystemEntry entry) => false;
}

public readonly record struct FilePathMatcher : IIncludeable<string>
{
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    public FilePathMatcher(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public bool Include(string path)
    {
        if (!Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fileInfo = new FileInfo(path);

        return fileInfo.LastWriteTime < _endDate && fileInfo.CreationTime > _startDate;
    }
}

public readonly record struct ZipArchiveEntryMatcher : IIncludeable<ZipArchiveEntry>
{
    private readonly DateTime _startDate;

    //private readonly DateTime _endDate;

    public ZipArchiveEntryMatcher(DateTime startDate)
    {
        _startDate = startDate;
    }

    public bool Include(ZipArchiveEntry value)
    {
        return value.LastWriteTime < _startDate;
    }
}
