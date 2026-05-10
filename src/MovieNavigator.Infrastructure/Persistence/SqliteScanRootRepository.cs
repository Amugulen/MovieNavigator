using Microsoft.Data.Sqlite;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Indexing;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Infrastructure.Persistence;

public sealed class SqliteScanRootRepository : IScanRootRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteScanRootRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task UpsertAsync(ScanRoot root, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        INSERT INTO scan_roots(path, library_type, enabled, last_scan_at)
        VALUES ($path, $libraryType, $enabled, $lastScanAt)
        ON CONFLICT(path) DO UPDATE SET
            library_type = excluded.library_type,
            enabled = excluded.enabled,
            last_scan_at = excluded.last_scan_at;
        """;
        AddParameters(command, root);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScanRoot>> GetEnabledAsync(MediaLibraryType libraryType, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        SELECT path, library_type, enabled, last_scan_at
        FROM scan_roots
        WHERE library_type = $libraryType AND enabled = 1
        ORDER BY path;
        """;
        command.Parameters.AddWithValue("$libraryType", (int)libraryType);

        var roots = new List<ScanRoot>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roots.Add(ReadRoot(reader));
        }

        return roots;
    }

    public async Task UpdateLastScanAtAsync(string path, DateTimeOffset scannedAt, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        UPDATE scan_roots
        SET last_scan_at = $lastScanAt
        WHERE path = $path;
        """;
        command.Parameters.AddWithValue("$path", path);
        command.Parameters.AddWithValue("$lastScanAt", scannedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(SqliteCommand command, ScanRoot root)
    {
        command.Parameters.AddWithValue("$path", root.Path);
        command.Parameters.AddWithValue("$libraryType", (int)root.LibraryType);
        command.Parameters.AddWithValue("$enabled", root.IsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$lastScanAt", (object?)root.LastScanAt?.ToString("O") ?? DBNull.Value);
    }

    private static ScanRoot ReadRoot(SqliteDataReader reader)
    {
        return new ScanRoot(
            reader.GetString(0),
            (MediaLibraryType)reader.GetInt32(1),
            reader.GetInt32(2) == 1,
            reader.IsDBNull(3) ? null : DateTimeOffset.Parse(reader.GetString(3)));
    }
}
