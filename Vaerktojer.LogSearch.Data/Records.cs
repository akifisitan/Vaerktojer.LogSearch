namespace Vaerktojer.LogSearch.Data;

public sealed record SearchResult(string FilePath, int LineNumber, string LineContent);

public sealed record ZipFileSearchOptions(string? ExtractPath = null, bool StopWhenFound = true)
{
    public static readonly ZipFileSearchOptions Default = new();
}

public sealed record FileSearchOptions(bool StopWhenFound = true)
{
    public static readonly FileSearchOptions Default = new();
}
