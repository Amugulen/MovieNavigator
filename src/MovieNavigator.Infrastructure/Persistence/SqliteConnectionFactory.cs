using Microsoft.Data.Sqlite;

namespace MovieNavigator.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _heldConnection;

    private SqliteConnectionFactory(string connectionString, SqliteConnection? heldConnection = null)
    {
        _connectionString = connectionString;
        _heldConnection = heldConnection;
    }

    public static SqliteConnectionFactory File(string path)
    {
        return new SqliteConnectionFactory($"Data Source={path}");
    }

    public static SqliteConnectionFactory InMemory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new SqliteConnectionFactory("Data Source=:memory:", connection);
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (_heldConnection is not null)
        {
            return _heldConnection;
        }

        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public ValueTask DisposeAsync()
    {
        return _heldConnection is null ? ValueTask.CompletedTask : _heldConnection.DisposeAsync();
    }
}
