using FluentAssertions;
using MovieNavigator.Core.Ai;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.Tests.Ai;

public sealed class AiSettingsTests
{
    [Fact]
    public async Task Load_returns_disabled_defaults_when_settings_have_not_been_saved()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var repository = new SqliteAppSettingsRepository(factory);

        var settings = await repository.LoadAiSettingsAsync(CancellationToken.None);

        settings.IsEnabled.Should().BeFalse();
        settings.Provider.Should().Be("OpenAI-compatible");
        settings.BaseUrl.Should().Be("http://localhost:11434/v1");
        settings.Model.Should().BeEmpty();
        settings.ApiKey.Should().BeNull();
        settings.HasApiKey.Should().BeFalse();
    }

    [Fact]
    public async Task Save_and_load_round_trips_ai_settings_without_losing_api_key_presence()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var firstRepository = new SqliteAppSettingsRepository(factory);
        var saved = new AiSettings(
            "OpenAI-compatible",
            "https://api.example.test/v1",
            "movie-classifier",
            IsEnabled: true,
            ApiKey: "secret-key");

        await firstRepository.SaveAiSettingsAsync(saved, saveApiKey: true, CancellationToken.None);

        var recreatedRepository = new SqliteAppSettingsRepository(factory);
        var loaded = await recreatedRepository.LoadAiSettingsAsync(CancellationToken.None);

        loaded.Provider.Should().Be(saved.Provider);
        loaded.BaseUrl.Should().Be(saved.BaseUrl);
        loaded.Model.Should().Be(saved.Model);
        loaded.IsEnabled.Should().BeTrue();
        loaded.ApiKey.Should().Be("secret-key");
        loaded.HasApiKey.Should().BeTrue();
        loaded.ToString().Should().NotContain("secret-key");
    }

    [Fact]
    public async Task Save_does_not_overwrite_existing_api_key_unless_user_explicitly_saves_it()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var repository = new SqliteAppSettingsRepository(factory);
        await repository.SaveAiSettingsAsync(
            new AiSettings("OpenAI-compatible", "https://api.example.test/v1", "old-model", true, "secret-key"),
            saveApiKey: true,
            CancellationToken.None);

        await repository.SaveAiSettingsAsync(
            new AiSettings("OpenAI-compatible", "https://api.example.test/v1", "new-model", false, null),
            saveApiKey: false,
            CancellationToken.None);

        var loaded = await repository.LoadAiSettingsAsync(CancellationToken.None);

        loaded.Model.Should().Be("new-model");
        loaded.IsEnabled.Should().BeFalse();
        loaded.ApiKey.Should().Be("secret-key");
        loaded.HasApiKey.Should().BeTrue();
    }
}
