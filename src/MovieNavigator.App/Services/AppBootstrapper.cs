using System.IO;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.App.Services;

public static class AppBootstrapper
{
    public static async Task<SqliteConnectionFactory> InitializeAsync(CancellationToken cancellationToken)
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MovieNavigator");
        Directory.CreateDirectory(appData);
        var databasePath = Path.Combine(appData, "normal.db");
        var factory = SqliteConnectionFactory.File(databasePath);
        await DatabaseInitializer.InitializeAsync(factory, cancellationToken);
        return factory;
    }
}
