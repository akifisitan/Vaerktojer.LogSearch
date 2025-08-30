namespace Vaerktojer.LogSearch.Abstractions;

public interface IIncludeable<T>
{
    bool Include(T value);
}
