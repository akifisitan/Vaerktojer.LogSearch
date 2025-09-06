namespace Vaerktojer.LogSearch.Lib.Abstractions;

public interface ILineMatcher
{
    bool IsMatch(string line);
}
