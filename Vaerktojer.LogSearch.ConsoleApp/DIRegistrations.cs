using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vaerktojer.LogSearch.Lib;
using ZLogger;

namespace Vaerktojer.LogSearch.ConsoleApp;

public static class DIRegistrations
{
    public static IServiceCollection AddLogSearchConsoleApp(this IServiceCollection services)
    {
        services.AddLogSearchLib();

        services.AddLogging(x =>
        {
            x.ClearProviders();
            x.SetMinimumLevel(LogLevel.Trace);
            x.AddZLoggerConsole();
            x.AddZLoggerFile("log.txt");
        });

        services.AddSingleton<App>();

        return services;
    }
}
