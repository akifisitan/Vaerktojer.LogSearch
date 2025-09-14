using Microsoft.Extensions.DependencyInjection;
using Vaerktojer.Logging.File;
using Vaerktojer.LogSearch.Lib;

namespace Vaerktojer.LogSearch.ConsoleApp;

public static class DIRegistrations
{
    private static readonly string appBasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Vaerktojer.LogSearch.ConsoleApp"
    );

    public static IServiceCollection AddLogSearchConsoleApp(this IServiceCollection services)
    {
        services.AddLogSearchLib();

        services.AddVaerktojerFileLogging(
            Path.Combine(appBasePath, "Logs"),
            enableConsoleLogging: false
#if DEBUG
            ,
            minimumLogLevel: Microsoft.Extensions.Logging.LogLevel.Trace
#endif
        );

        services.AddSingleton<App>();

        return services;
    }
}
