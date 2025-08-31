using System.IO.Compression;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using Vaerktojer.LogSearch.Abstractions;

namespace Vaerktojer.LogSearch.Lib;

public readonly record struct StringContainsLineMatcher : ILineMatcher
{
    private readonly string _value;

    public StringContainsLineMatcher(string value)
    {
        _value = value;
    }

    public bool IsMatch(string line) => line.Contains(_value);
}

public readonly record struct RegexLineMatcher : ILineMatcher
{
    private readonly Regex _value;

    public RegexLineMatcher(string pattern)
    {
        _value = new Regex(
            pattern,
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking
        );
    }

    public bool IsMatch(string line) => _value.IsMatch(line);
}

public readonly record struct ZipFileEnumerationFilter : IFileSystemEnumerationFilter
{
    public bool IncludeFile(ref FileSystemEntry entry) =>
        Path.GetExtension(entry.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    public bool ExcludeDirectory(ref FileSystemEntry entry) => false;
}

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
