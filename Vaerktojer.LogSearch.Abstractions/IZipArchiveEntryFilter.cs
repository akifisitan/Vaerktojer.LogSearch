using System.IO.Compression;

namespace Vaerktojer.LogSearch.Abstractions;

public interface IZipArchiveEntryFilter
{
    bool Include(ZipArchiveEntry entry);
}
