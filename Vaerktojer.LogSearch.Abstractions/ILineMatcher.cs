namespace Vaerktojer.LogSearch.Abstractions;

public interface ILineMatcher
{
    bool Match(string line);
}
