using System.Diagnostics;

namespace Vaerktojer.LogSearch.Lib;

public static class Utils
{
    public static void OpenWithNpp(string path)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = @"C:\Program Files\Notepad++\notepad++.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        );
    }
}
