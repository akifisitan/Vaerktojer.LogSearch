using System.IO.Enumeration;
using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Filters;

public readonly record struct LogFileEnumerationFilter : IFileSystemEnumerationFilter
{
    private readonly DateTimeOffset _startDateUtc;
    private readonly DateTimeOffset _endDateUtc;

    public LogFileEnumerationFilter(DateTimeOffset startDateUtc, DateTimeOffset endDateUtc)
    {
        _startDateUtc = startDateUtc;
        _endDateUtc = endDateUtc;
    }

    public bool IncludeFile(ref FileSystemEntry entry) =>
        Path.GetExtension(entry.FileName).Equals(".log", StringComparison.OrdinalIgnoreCase)
        && entry.LastWriteTimeUtc <= _endDateUtc
        && entry.CreationTimeUtc >= _startDateUtc;

    public bool ExcludeDirectory(ref FileSystemEntry entry) => false;
}
