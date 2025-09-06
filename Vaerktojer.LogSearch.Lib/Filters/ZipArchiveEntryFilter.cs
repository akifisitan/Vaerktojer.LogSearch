using System.IO.Compression;
using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Filters;

public readonly record struct ZipArchiveEntryFilter : IZipArchiveEntryFilter
{
    private readonly DateTimeOffset _startDate;

    public ZipArchiveEntryFilter(DateTimeOffset startDate)
    {
        _startDate = startDate;
    }

    public bool Include(ZipArchiveEntry entry)
    {
        return Path.GetExtension(entry.FullName).Equals(".log", StringComparison.OrdinalIgnoreCase)
            && entry.LastWriteTime < _startDate;
    }
}
