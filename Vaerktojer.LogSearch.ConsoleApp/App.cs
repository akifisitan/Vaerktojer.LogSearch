namespace Vaerktojer.LogSearch.ConsoleApp;

internal class App
{
    public async Task Run()
    {
        var v = Prompt.Prompt.MultiSelect<string>("123");
    }
}
