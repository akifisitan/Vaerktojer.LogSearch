namespace Vaerktojer.LogSearch.Abstractions;

public interface ILineMatcher
{
    bool IsMatch(string line);
}
