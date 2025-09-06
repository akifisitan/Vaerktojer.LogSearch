using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Matchers;

public readonly record struct SpanLineMatcher : ISpanLineMatcher
{
    private readonly string _value;

    public SpanLineMatcher(string value)
    {
        _value = value;
    }

    public bool IsMatch(ReadOnlySpan<char> line)
    {
        return line.Contains(_value, StringComparison.OrdinalIgnoreCase);
    }
}
