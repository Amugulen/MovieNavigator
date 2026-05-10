using Microsoft.Data.Sqlite;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Infrastructure.Persistence;

public sealed class SqliteMediaRepository : IMediaRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteMediaRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task UpsertAsync(MediaItem item, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var upsert = connection.CreateCommand();
        upsert.Transaction = (SqliteTransaction)transaction;
        upsert.CommandText = """
        INSERT INTO media_items(id, library_type, status, file_path, file_name, drive_key, size_bytes, duration_seconds, width, height, title, original_title, year, summary, thumbnail_path, extension, last_write_time_utc, missing_since, created_at, updated_at)
        VALUES ($id, $libraryType, $status, $filePath, $fileName, $driveKey, $sizeBytes, $durationSeconds, $width, $height, $title, $originalTitle, $year, $summary, $thumbnailPath, $extension, $lastWriteTimeUtc, $missingSince, $createdAt, $updatedAt)
        ON CONFLICT(file_path) DO UPDATE SET
            library_type = excluded.library_type,
            status = excluded.status,
            file_name = excluded.file_name,
            drive_key = excluded.drive_key,
            size_bytes = excluded.size_bytes,
            duration_seconds = excluded.duration_seconds,
            width = excluded.width,
            height = excluded.height,
            title = excluded.title,
            original_title = excluded.original_title,
            year = excluded.year,
            summary = excluded.summary,
            thumbnail_path = excluded.thumbnail_path,
            extension = excluded.extension,
            last_write_time_utc = excluded.last_write_time_utc,
            missing_since = excluded.missing_since,
            updated_at = excluded.updated_at;
        """;
        AddMediaParameters(upsert, item);
        await upsert.ExecuteNonQueryAsync(cancellationToken);

        var deleteTags = connection.CreateCommand();
        deleteTags.Transaction = (SqliteTransaction)transaction;
        deleteTags.CommandText = "DELETE FROM media_tags WHERE media_id = $id;";
        deleteTags.Parameters.AddWithValue("$id", item.Id.ToString());
        await deleteTags.ExecuteNonQueryAsync(cancellationToken);

        foreach (var tag in item.Tags)
        {
            var insertTag = connection.CreateCommand();
            insertTag.Transaction = (SqliteTransaction)transaction;
            insertTag.CommandText = "INSERT OR IGNORE INTO media_tags(media_id, tag_key) VALUES ($id, $tagKey);";
            insertTag.Parameters.AddWithValue("$id", item.Id.ToString());
            insertTag.Parameters.AddWithValue("$tagKey", tag.Value);
            await insertTag.ExecuteNonQueryAsync(cancellationToken);
        }

        var deleteSearch = connection.CreateCommand();
        deleteSearch.Transaction = (SqliteTransaction)transaction;
        deleteSearch.CommandText = "DELETE FROM media_search WHERE media_id = $id;";
        deleteSearch.Parameters.AddWithValue("$id", item.Id.ToString());
        await deleteSearch.ExecuteNonQueryAsync(cancellationToken);

        var insertSearch = connection.CreateCommand();
        insertSearch.Transaction = (SqliteTransaction)transaction;
        insertSearch.CommandText = "INSERT INTO media_search(media_id, content) VALUES ($id, $content);";
        insertSearch.Parameters.AddWithValue("$id", item.Id.ToString());
        insertSearch.Parameters.AddWithValue("$content", await BuildSearchContentAsync(connection, (SqliteTransaction)transaction, item, cancellationToken));
        await insertSearch.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaItem>> GetAllAsync(MediaLibraryType libraryType, bool includeAdultWhenUnlocked, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        SELECT id, library_type, status, file_path, file_name, drive_key, size_bytes, duration_seconds, width, height, title, original_title, year, summary, created_at, updated_at, thumbnail_path, extension, last_write_time_utc, missing_since
        FROM media_items
        WHERE library_type = $libraryType
          AND ($includeAdult = 1 OR library_type <> 1)
        ORDER BY updated_at DESC;
        """;
        command.Parameters.AddWithValue("$libraryType", (int)libraryType);
        command.Parameters.AddWithValue("$includeAdult", includeAdultWhenUnlocked ? 1 : 0);
        return await ReadMediaItemsAsync(connection, command, cancellationToken);
    }

    public async Task<MediaItem?> GetByPathAsync(string filePath, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        SELECT id, library_type, status, file_path, file_name, drive_key, size_bytes, duration_seconds, width, height, title, original_title, year, summary, created_at, updated_at, thumbnail_path, extension, last_write_time_utc, missing_since
        FROM media_items
        WHERE file_path = $filePath
        LIMIT 1;
        """;
        command.Parameters.AddWithValue("$filePath", filePath);
        return (await ReadMediaItemsAsync(connection, command, cancellationToken)).SingleOrDefault();
    }

    public async Task<IReadOnlyList<MediaItem>> SearchAsync(string query, MediaLibraryType libraryType, bool includeAdultWhenUnlocked, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        SELECT m.id, m.library_type, m.status, m.file_path, m.file_name, m.drive_key, m.size_bytes, m.duration_seconds, m.width, m.height, m.title, m.original_title, m.year, m.summary, m.created_at, m.updated_at, m.thumbnail_path, m.extension, m.last_write_time_utc, m.missing_since
        FROM media_items m
        JOIN media_search s ON s.media_id = m.id
        WHERE s.content MATCH $query
          AND m.library_type = $libraryType
          AND ($includeAdult = 1 OR m.library_type <> 1)
        ORDER BY m.updated_at DESC;
        """;
        command.Parameters.AddWithValue("$query", EscapeFtsQuery(query));
        command.Parameters.AddWithValue("$libraryType", (int)libraryType);
        command.Parameters.AddWithValue("$includeAdult", includeAdultWhenUnlocked ? 1 : 0);
        var ftsResults = await ReadMediaItemsAsync(connection, command, cancellationToken);
        if (ftsResults.Count > 0)
        {
            return ftsResults;
        }

        var fallback = connection.CreateCommand();
        fallback.CommandText = """
        SELECT m.id, m.library_type, m.status, m.file_path, m.file_name, m.drive_key, m.size_bytes, m.duration_seconds, m.width, m.height, m.title, m.original_title, m.year, m.summary, m.created_at, m.updated_at, m.thumbnail_path, m.extension, m.last_write_time_utc, m.missing_since
        FROM media_items m
        JOIN media_search s ON s.media_id = m.id
        WHERE s.content LIKE $likeQuery
          AND m.library_type = $libraryType
          AND ($includeAdult = 1 OR m.library_type <> 1)
        ORDER BY m.updated_at DESC;
        """;
        fallback.Parameters.AddWithValue("$likeQuery", $"%{query}%");
        fallback.Parameters.AddWithValue("$libraryType", (int)libraryType);
        fallback.Parameters.AddWithValue("$includeAdult", includeAdultWhenUnlocked ? 1 : 0);
        return await ReadMediaItemsAsync(connection, fallback, cancellationToken);
    }

    public async Task<IReadOnlyList<MediaItem>> GetByDriveAsync(string driveKey, MediaLibraryType libraryType, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        SELECT id, library_type, status, file_path, file_name, drive_key, size_bytes, duration_seconds, width, height, title, original_title, year, summary, created_at, updated_at, thumbnail_path, extension, last_write_time_utc, missing_since
        FROM media_items
        WHERE drive_key = $driveKey AND library_type = $libraryType
        ORDER BY file_path;
        """;
        command.Parameters.AddWithValue("$driveKey", driveKey);
        command.Parameters.AddWithValue("$libraryType", (int)libraryType);
        return await ReadMediaItemsAsync(connection, command, cancellationToken);
    }

    public async Task MarkMissingAsync(string filePath, DateTimeOffset missingSince, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        UPDATE media_items
        SET status = $status,
            missing_since = $missingSince,
            updated_at = $updatedAt
        WHERE file_path = $filePath;
        """;
        command.Parameters.AddWithValue("$status", (int)MediaStatus.Offline);
        command.Parameters.AddWithValue("$missingSince", missingSince.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", missingSince.ToString("O"));
        command.Parameters.AddWithValue("$filePath", filePath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddMediaParameters(SqliteCommand command, MediaItem item)
    {
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$libraryType", (int)item.LibraryType);
        command.Parameters.AddWithValue("$status", (int)item.Status);
        command.Parameters.AddWithValue("$filePath", item.FilePath);
        command.Parameters.AddWithValue("$fileName", item.FileName);
        command.Parameters.AddWithValue("$driveKey", item.DriveKey);
        command.Parameters.AddWithValue("$sizeBytes", item.SizeBytes);
        command.Parameters.AddWithValue("$durationSeconds", item.Duration.TotalSeconds);
        command.Parameters.AddWithValue("$width", (object?)item.Width ?? DBNull.Value);
        command.Parameters.AddWithValue("$height", (object?)item.Height ?? DBNull.Value);
        command.Parameters.AddWithValue("$title", (object?)item.Title ?? DBNull.Value);
        command.Parameters.AddWithValue("$originalTitle", (object?)item.OriginalTitle ?? DBNull.Value);
        command.Parameters.AddWithValue("$year", (object?)item.Year ?? DBNull.Value);
        command.Parameters.AddWithValue("$summary", (object?)item.Summary ?? DBNull.Value);
        command.Parameters.AddWithValue("$thumbnailPath", (object?)item.ThumbnailPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$extension", (object?)item.Extension ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastWriteTimeUtc", (object?)item.LastWriteTimeUtc?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("$missingSince", (object?)item.MissingSince?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", item.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", item.UpdatedAt.ToString("O"));
    }

    private static async Task<string> BuildSearchContentAsync(SqliteConnection connection, SqliteTransaction transaction, MediaItem item, CancellationToken cancellationToken)
    {
        var tagText = new List<string>();
        foreach (var tag in item.Tags)
        {
            tagText.Add(tag.Value);
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT display_zh, display_en, aliases FROM tags WHERE key = $key;";
            command.Parameters.AddWithValue("$key", tag.Value);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                tagText.Add(reader.GetString(0));
                tagText.Add(reader.GetString(1));
                tagText.AddRange(reader.GetString(2).Split('|', StringSplitOptions.RemoveEmptyEntries));
            }
        }

        return string.Join(' ', new[]
        {
            item.FilePath,
            item.FileName,
            item.Title,
            item.OriginalTitle,
            item.Year?.ToString(),
            item.Summary,
            string.Join(' ', tagText)
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string EscapeFtsQuery(string query)
    {
        return "\"" + query.Replace("\"", "\"\"") + "\"";
    }

    private static async Task<IReadOnlyList<MediaItem>> ReadMediaItemsAsync(SqliteConnection connection, SqliteCommand command, CancellationToken cancellationToken)
    {
        var items = new List<MediaItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MediaItem(
                Guid.Parse(reader.GetString(0)),
                (MediaLibraryType)reader.GetInt32(1),
                (MediaStatus)reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetInt64(6),
                TimeSpan.FromSeconds(reader.GetDouble(7)),
                reader.IsDBNull(8) ? null : reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetInt32(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                await ReadTagsAsync(connection, reader.GetString(0), cancellationToken),
                DateTimeOffset.Parse(reader.GetString(14)),
                DateTimeOffset.Parse(reader.GetString(15)),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.IsDBNull(17) ? null : reader.GetString(17),
                reader.IsDBNull(18) ? null : DateTimeOffset.Parse(reader.GetString(18)),
                reader.IsDBNull(19) ? null : DateTimeOffset.Parse(reader.GetString(19))));
        }

        return items;
    }

    private static async Task<IReadOnlyCollection<TagKey>> ReadTagsAsync(SqliteConnection connection, string mediaId, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
        SELECT tag_key
        FROM media_tags
        WHERE media_id = $mediaId
        ORDER BY tag_key;
        """;
        command.Parameters.AddWithValue("$mediaId", mediaId);

        var tags = new List<TagKey>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tags.Add(TagKey.Parse(reader.GetString(0)));
        }

        return tags;
    }
}
