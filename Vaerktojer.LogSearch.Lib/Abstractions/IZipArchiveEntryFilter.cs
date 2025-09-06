using System.IO.Compression;

namespace Vaerktojer.LogSearch.Lib.Abstractions;

public interface IZipArchiveEntryFilter
{
    bool Include(ZipArchiveEntry entry);
}
