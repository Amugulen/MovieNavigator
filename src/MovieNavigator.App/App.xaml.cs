using System.Windows;
using MovieNavigator.App.Services;

namespace MovieNavigator.App;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var factory = await AppBootstrapper.InitializeAsync(CancellationToken.None);
        var window = new MainWindow(factory);
        window.Show();
    }
}
