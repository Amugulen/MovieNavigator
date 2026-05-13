using Microsoft.Data.Sqlite;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Ai;

namespace MovieNavigator.Infrastructure.Persistence;

public sealed class SqliteAppSettingsRepository : IAiSettingsRepository
{
    private const string ProviderKey = "ai.provider";
    private const string BaseUrlKey = "ai.base_url";
    private const string ModelKey = "ai.model";
    private const string EnabledKey = "ai.enabled";
    private const string ApiKeyKey = "ai.api_key";

    private readonly SqliteConnectionFactory _factory;

    public SqliteAppSettingsRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<AiSettings> LoadAiSettingsAsync(CancellationToken cancellationToken)
    {
        var values = await LoadAllAsync(cancellationToken);
        return new AiSettings(
            values.GetValueOrDefault(ProviderKey, AiSettings.DefaultProvider),
            values.GetValueOrDefault(BaseUrlKey, AiSettings.DefaultBaseUrl),
            values.GetValueOrDefault(ModelKey, string.Empty),
            values.TryGetValue(EnabledKey, out var enabled) && enabled == "1",
            values.GetValueOrDefault(ApiKeyKey));
    }

    public async Task SaveAiSettingsAsync(
        AiSettings settings,
        bool saveApiKey,
        CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await UpsertAsync(connection, (SqliteTransaction)transaction, ProviderKey, settings.Provider, cancellationToken);
        await UpsertAsync(connection, (SqliteTransaction)transaction, BaseUrlKey, settings.BaseUrl, cancellationToken);
        await UpsertAsync(connection, (SqliteTransaction)transaction, ModelKey, settings.Model, cancellationToken);
        await UpsertAsync(connection, (SqliteTransaction)transaction, EnabledKey, settings.IsEnabled ? "1" : "0", cancellationToken);

        if (saveApiKey)
        {
            await UpsertAsync(connection, (SqliteTransaction)transaction, ApiKeyKey, settings.ApiKey ?? string.Empty, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT key, value FROM app_settings;";

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values[reader.GetString(0)] = reader.GetString(1);
        }

        return values;
    }

    private static async Task UpsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
        INSERT INTO app_settings(key, value)
        VALUES ($key, $value)
        ON CONFLICT(key) DO UPDATE SET value = excluded.value;
        """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
