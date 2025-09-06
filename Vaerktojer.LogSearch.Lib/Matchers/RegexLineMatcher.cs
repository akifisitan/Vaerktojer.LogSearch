using System.Text.RegularExpressions;
using Vaerktojer.LogSearch.Lib.Abstractions;

namespace Vaerktojer.LogSearch.Lib.Matchers;

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
