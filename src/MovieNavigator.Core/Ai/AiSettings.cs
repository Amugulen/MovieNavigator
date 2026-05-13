namespace MovieNavigator.Core.Ai;

public sealed record AiSettings(
    string Provider,
    string BaseUrl,
    string Model,
    bool IsEnabled,
    string? ApiKey = null)
{
    public const string DefaultProvider = "OpenAI-compatible";
    public const string DefaultBaseUrl = "http://localhost:11434/v1";

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public static AiSettings DisabledDefault()
    {
        return new AiSettings(DefaultProvider, DefaultBaseUrl, string.Empty, IsEnabled: false);
    }

    public override string ToString()
    {
        return $"{Provider} {BaseUrl} {Model} Enabled={IsEnabled} HasApiKey={HasApiKey}";
    }
}
