using System.Diagnostics;
using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Infrastructure.FileSystem;

public sealed class DefaultProcessLauncher : IProcessLauncher
{
    public void OpenWithDefaultApplication(string path)
    {
        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true
        });
    }

    public void OpenFolder(string folderPath)
    {
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folderPath}\"")
        {
            UseShellExecute = true
        });
    }
}
