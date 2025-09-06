using Microsoft.Extensions.DependencyInjection;
using Vaerktojer.LogSearch.ConsoleApp;

static async Task Demo()
{
    Console.WriteLine(Console.BufferWidth);

    //Console.BufferWidth += 50;
    Console.WindowWidth += 100;
    Console.WriteLine("wowzerz");

    while (true)
    {
        await Task.Delay(1000);
        Console.SetBufferSize(3, 5);
        Console.WriteLine($"{Console.BufferWidth}, {Console.BufferHeight}");
        Console.WindowWidth += 5;
        //var key = Console.ReadLine();
        //Console.SetCursorPosition(int.Parse(key.Split(',')[0]), int.Parse(key.Split(',')[1]));
    }
}

var app = new ServiceCollection()
    .AddLogSearchConsoleApp()
    .BuildServiceProvider(validateScopes: true)
    .GetRequiredService<App>();

await app.Run();
