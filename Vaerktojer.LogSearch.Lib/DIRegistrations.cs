using Microsoft.Extensions.DependencyInjection;
using Vaerktojer.LogSearch.Lib.Core;

namespace Vaerktojer.LogSearch.Lib;

public static class DIRegistrations
{
    public static IServiceCollection AddLogSearchLib(this IServiceCollection services)
    {
        services.AddSingleton<FileSearcher>();
        services.AddSingleton<ZipFileSearcher>();

        return services;
    }
}
