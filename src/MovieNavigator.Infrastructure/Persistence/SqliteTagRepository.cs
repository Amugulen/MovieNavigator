using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Infrastructure.Persistence;

public sealed class SqliteTagRepository : ITagRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteTagRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task UpsertAsync(TagDefinition tag, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        INSERT INTO tags(key, display_zh, display_en, aliases, parent_key)
        VALUES ($key, $displayZh, $displayEn, $aliases, $parentKey)
        ON CONFLICT(key) DO UPDATE SET
            display_zh = excluded.display_zh,
            display_en = excluded.display_en,
            aliases = excluded.aliases,
            parent_key = excluded.parent_key;
        """;
        command.Parameters.AddWithValue("$key", tag.Key.Value);
        command.Parameters.AddWithValue("$displayZh", tag.DisplayNameZh);
        command.Parameters.AddWithValue("$displayEn", tag.DisplayNameEn);
        command.Parameters.AddWithValue("$aliases", string.Join("|", tag.Aliases));
        command.Parameters.AddWithValue("$parentKey", (object?)tag.ParentKey?.Value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TagDefinition>> GetTreeAsync(CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT key, display_zh, display_en, aliases, parent_key FROM tags ORDER BY key;";
        var tags = new List<TagDefinition>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var parent = reader.IsDBNull(4) ? (TagKey?)null : TagKey.Parse(reader.GetString(4));
            tags.Add(new TagDefinition(
                TagKey.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3).Split('|', StringSplitOptions.RemoveEmptyEntries),
                parent));
        }

        return tags;
    }
}
