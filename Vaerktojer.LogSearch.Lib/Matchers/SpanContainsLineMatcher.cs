using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Matchers;

public readonly record struct SpanContainsLineMatcher : ISpanLineMatcher
{
    private readonly string _value;

    public SpanContainsLineMatcher(string value)
    {
        _value = value;
    }

    public bool IsMatch(ReadOnlySpan<char> line)
    {
        return line.Contains(_value, StringComparison.OrdinalIgnoreCase);
    }
}
