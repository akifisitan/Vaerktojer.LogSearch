using Microsoft.Extensions.DependencyInjection;
using Vaerktojer.LogSearch.ConsoleApp;

var app = new ServiceCollection()
    .AddLogSearchConsoleApp()
    .BuildServiceProvider(validateScopes: true)
    .GetRequiredService<App>();

await app.Run();
