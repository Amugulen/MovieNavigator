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
        """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
