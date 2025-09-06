using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Matchers;

public readonly record struct StringContainsLineMatcher : ILineMatcher
{
    private readonly string _value;

    public StringContainsLineMatcher(string value)
    {
        _value = value;
    }

    public bool IsMatch(string line) => line.Contains(_value);
}
