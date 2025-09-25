using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vaerktojer.LogSearch.Lib.Core;

namespace Vaerktojer.LogSearch.Lib;

public static class DIRegistrations
{
    public static IServiceCollection AddLogSearchLib(this IServiceCollection services)
    {
        services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSingleton<FileSearcher>();
        services.AddSingleton<ZipFileSearcher>();

        return services;
    }
}
