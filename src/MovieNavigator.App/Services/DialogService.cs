using System.Windows;

namespace MovieNavigator.App.Services;

public sealed class DialogService
{
    public void ShowMessage(string message, string caption)
    {
        System.Windows.MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
