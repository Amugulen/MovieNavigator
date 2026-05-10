using System.Windows;
using MovieNavigator.App.Services;

namespace MovieNavigator.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await AppBootstrapper.InitializeAsync(CancellationToken.None);
        var window = new MainWindow();
        window.Show();
    }
}
