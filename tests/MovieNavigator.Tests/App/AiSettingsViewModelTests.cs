using FluentAssertions;
using MovieNavigator.App.ViewModels;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Ai;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Tests.App;

public sealed class AiSettingsViewModelTests
{
    [Fact]
    public async Task Load_populates_disabled_defaults_and_warning_text()
    {
        var repository = new InMemoryAiSettingsRepository();
        var viewModel = new AiSettingsViewModel(repository);

        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.IsEnabled.Should().BeFalse();
        viewModel.Provider.Should().Be("OpenAI-compatible");
        viewModel.BaseUrl.Should().Be("http://localhost:11434/v1");
        viewModel.Model.Should().BeEmpty();
        viewModel.WarningText.Should().Be("当前版本只发送文本线索，不发送截图、视频、音频。成人库建议使用单独配置。");
    }

    [Fact]
    public async Task Save_without_api_key_change_preserves_existing_secret()
    {
        var repository = new InMemoryAiSettingsRepository();
        await repository.SaveAiSettingsAsync(
            new AiSettings("OpenAI-compatible", "https://api.example.test/v1", "old-model", true, "secret-key"),
            saveApiKey: true,
            CancellationToken.None);
        var viewModel = new AiSettingsViewModel(repository);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.Model = "new-model";
        viewModel.ApiKey = string.Empty;

        await viewModel.SaveAsync(CancellationToken.None);

        repository.Current.Model.Should().Be("new-model");
        repository.Current.ApiKey.Should().Be("secret-key");
        viewModel.StatusMessage.Should().Be("AI 设置已保存。");
    }

    [Fact]
    public async Task Test_connection_does_not_call_ai_when_disabled_or_incomplete()
    {
        var repository = new InMemoryAiSettingsRepository();
        var viewModel = new AiSettingsViewModel(repository);
        await viewModel.LoadAsync(CancellationToken.None);

        var result = await viewModel.TestConnectionAsync(CancellationToken.None);

        result.Should().BeFalse();
        viewModel.StatusMessage.Should().Contain("启用 AI");
    }

    [Fact]
    public async Task Test_connection_calls_ai_client_with_text_only_probe_when_enabled()
    {
        var repository = new InMemoryAiSettingsRepository
        {
            Current = new AiSettings(
                "OpenAI-compatible",
                "https://api.example.test/v1",
                "movie-classifier",
                true,
                "secret-key")
        };
        var client = new CapturingAiClassificationClient();
        var viewModel = new AiSettingsViewModel(repository, client);
        await viewModel.LoadAsync(CancellationToken.None);

        var result = await viewModel.TestConnectionAsync(CancellationToken.None);

        result.Should().BeTrue();
        client.Settings.Should().NotBeNull();
        client.Settings!.ApiKey.Should().Be("secret-key");
        client.Request.Should().NotBeNull();
        client.Request!.FileName.Should().Be("connection-test.txt");
        client.Request.LibraryType.Should().Be(MediaLibraryType.Normal);
        client.Request.ToConfirmationLines().Should().NotContain(line => line.Contains("截图", StringComparison.OrdinalIgnoreCase));
        viewModel.StatusMessage.Should().Be("AI 连接测试成功。");
    }

    [Fact]
    public async Task Test_connection_uses_current_unsaved_fields_and_typed_api_key()
    {
        var repository = new InMemoryAiSettingsRepository();
        var client = new CapturingAiClassificationClient();
        var viewModel = new AiSettingsViewModel(repository, client);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.IsEnabled = true;
        viewModel.BaseUrl = "https://api.current.test/v1";
        viewModel.Model = "current-model";
        viewModel.ApiKey = "typed-key";

        await viewModel.TestConnectionAsync(CancellationToken.None);

        client.Settings.Should().NotBeNull();
        client.Settings!.BaseUrl.Should().Be("https://api.current.test/v1");
        client.Settings.Model.Should().Be("current-model");
        client.Settings.ApiKey.Should().Be("typed-key");
    }

    private sealed class InMemoryAiSettingsRepository : IAiSettingsRepository
    {
        public AiSettings Current { get; set; } = AiSettings.DisabledDefault();

        public Task<AiSettings> LoadAiSettingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Current);
        }

        public Task SaveAiSettingsAsync(AiSettings settings, bool saveApiKey, CancellationToken cancellationToken)
        {
            Current = settings with
            {
                ApiKey = saveApiKey ? settings.ApiKey : Current.ApiKey
            };
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingAiClassificationClient : IAiClassificationClient
    {
        public AiSettings? Settings { get; private set; }
        public AiClassificationRequest? Request { get; private set; }

        public Task<AiClassificationSuggestion> SuggestAsync(
            AiSettings settings,
            AiClassificationRequest request,
            CancellationToken cancellationToken)
        {
            Settings = settings;
            Request = request;
            return Task.FromResult(new AiClassificationSuggestion("Probe", null, null, [], 1.0, null));
        }
    }
}
