namespace Vaerktojer.LogSearch.Lib.Abstractions;

public interface ISpanLineMatcher
{
    bool IsMatch(ReadOnlySpan<char> line);
}
