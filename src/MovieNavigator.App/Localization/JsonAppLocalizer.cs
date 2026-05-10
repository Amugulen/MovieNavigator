using System.IO;
using System.Text.Json;

namespace MovieNavigator.App.Localization;

public sealed class JsonAppLocalizer : IAppLocalizer
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _resources;

    private JsonAppLocalizer(string cultureName, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> resources)
    {
        CultureName = cultureName;
        _resources = resources;
    }

    public string CultureName { get; }

    public static JsonAppLocalizer FromDictionaries(string cultureName, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> resources)
    {
        return new JsonAppLocalizer(cultureName, resources);
    }

    public static async Task<JsonAppLocalizer> LoadAsync(string resourcesDirectory, string cultureName, CancellationToken cancellationToken)
    {
        var resources = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var culture in new[] { "zh-CN", "en-US" })
        {
            var path = Path.Combine(resourcesDirectory, $"Strings.{culture}.json");
            await using var stream = File.OpenRead(path);
            var values = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: cancellationToken);
            resources[culture] = values ?? new Dictionary<string, string>();
        }

        return new JsonAppLocalizer(cultureName, resources);
    }

    public string Get(string key)
    {
        if (_resources.TryGetValue(CultureName, out var current) && current.TryGetValue(key, out var localized))
        {
            return localized;
        }

        if (_resources.TryGetValue("en-US", out var english) && english.TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }
}
