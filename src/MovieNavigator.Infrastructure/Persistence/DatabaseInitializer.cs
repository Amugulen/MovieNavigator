namespace MovieNavigator.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(SqliteConnectionFactory factory, CancellationToken cancellationToken)
    {
        var connection = await factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        CREATE TABLE IF NOT EXISTS media_items (
            id TEXT PRIMARY KEY,
            library_type INTEGER NOT NULL,
            status INTEGER NOT NULL,
            file_path TEXT NOT NULL UNIQUE,
            file_name TEXT NOT NULL,
            drive_key TEXT NOT NULL,
            size_bytes INTEGER NOT NULL,
            duration_seconds REAL NOT NULL,
            width INTEGER NULL,
            height INTEGER NULL,
            title TEXT NULL,
            original_title TEXT NULL,
            year INTEGER NULL,
            summary TEXT NULL,
            thumbnail_path TEXT NULL,
            extension TEXT NULL,
            last_write_time_utc TEXT NULL,
            missing_since TEXT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS tags (
            key TEXT PRIMARY KEY,
            display_zh TEXT NOT NULL,
            display_en TEXT NOT NULL,
            aliases TEXT NOT NULL,
            parent_key TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS media_tags (
            media_id TEXT NOT NULL,
            tag_key TEXT NOT NULL,
            PRIMARY KEY (media_id, tag_key)
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS media_search USING fts5(
            media_id UNINDEXED,
            content
        );

        CREATE TABLE IF NOT EXISTS scan_roots (
            path TEXT PRIMARY KEY,
            library_type INTEGER NOT NULL,
            enabled INTEGER NOT NULL,
            last_scan_at TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS app_settings (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );
        """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        await EnsureColumnAsync(factory, "media_items", "thumbnail_path", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(factory, "media_items", "extension", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(factory, "media_items", "last_write_time_utc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(factory, "media_items", "missing_since", "TEXT NULL", cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        SqliteConnectionFactory factory,
        string tableName,
        string columnName,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        var connection = await factory.OpenAsync(cancellationToken);
        var pragma = connection.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await pragma.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }
}
