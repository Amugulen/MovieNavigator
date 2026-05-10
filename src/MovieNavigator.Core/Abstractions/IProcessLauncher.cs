namespace MovieNavigator.Core.Abstractions;

public interface IProcessLauncher
{
    void OpenWithDefaultApplication(string path);
    void OpenFolder(string folderPath);
}
