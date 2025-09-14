using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Formatters;

namespace Vaerktojer.Logging.File;

public static class DIRegistrations
{
    public static IServiceCollection AddVaerktojerFileLogging(
        this IServiceCollection services,
        string basePath,
        LogLevel minimumLogLevel = LogLevel.Information,
        bool enableConsoleLogging = false,
        int rollingSizeKB = 1024 * 1024
    )
    {
        services.AddLogging(x =>
        {
            x.ClearProviders();

            x.SetMinimumLevel(minimumLogLevel);

            if (enableConsoleLogging)
            {
                x.AddZLoggerConsole(options =>
                {
                    options.UsePlainTextFormatter(ConfigureFormatter);
                    options.OutputEncodingToUtf8 = true;
                    options.LogToStandardErrorThreshold = LogLevel.Warning;
                });
            }

            x.AddZLoggerRollingFile(options =>
            {
                options.UsePlainTextFormatter(ConfigureFormatter);
                options.RollingInterval = ZLogger.Providers.RollingInterval.Day;
                options.FilePathSelector = (utcDt, fileNumber) =>
                {
                    var localDt = utcDt.ToLocalTime();
                    return Path.Combine(
                        basePath,
                        $"{localDt:yyyy-MM-dd}",
                        $"{localDt:yyyy-MM-dd}_{fileNumber}.log"
                    );
                };
                options.RollingSizeKB = rollingSizeKB;
            });

            static void ConfigureFormatter(PlainTextZLoggerFormatter formatter)
            {
                formatter.SetPrefixFormatter(
                    $"[{0:local-longdate}] [{1}] [{2}] ",
                    (in MessageTemplate template, in LogInfo logInfo) =>
                        template.Format(logInfo.Timestamp, logInfo.LogLevel, logInfo.Category)
                );
            }
        });

        return services;
    }
}
