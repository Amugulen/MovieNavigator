using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Ai;
using MovieNavigator.Core.Media;

namespace MovieNavigator.App.ViewModels;

public sealed class AiSettingsViewModel : INotifyPropertyChanged
{
    public const string TextOnlyWarning = "当前版本只发送文本线索，不发送截图、视频、音频。成人库建议使用单独配置。";

    private readonly IAiSettingsRepository _settingsRepository;
    private readonly IAiClassificationClient? _classificationClient;
    private string _provider = AiSettings.DefaultProvider;
    private string _baseUrl = AiSettings.DefaultBaseUrl;
    private string _model = string.Empty;
    private string _apiKey = string.Empty;
    private bool _hasSavedApiKey;
    private bool _isEnabled;
    private string _statusMessage = "AI 默认关闭。保存设置后可测试连接。";

    public AiSettingsViewModel(
        IAiSettingsRepository settingsRepository,
        IAiClassificationClient? classificationClient = null)
    {
        _settingsRepository = settingsRepository;
        _classificationClient = classificationClient;
    }

    public string WarningText => TextOnlyWarning;

    public string Provider
    {
        get => _provider;
        set => SetField(ref _provider, value);
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => SetField(ref _baseUrl, value);
    }

    public string Model
    {
        get => _model;
        set => SetField(ref _model, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetField(ref _apiKey, value);
    }

    public bool HasSavedApiKey
    {
        get => _hasSavedApiKey;
        private set => SetField(ref _hasSavedApiKey, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.LoadAiSettingsAsync(cancellationToken);
        Provider = settings.Provider;
        BaseUrl = settings.BaseUrl;
        Model = settings.Model;
        ApiKey = string.Empty;
        HasSavedApiKey = settings.HasApiKey;
        IsEnabled = settings.IsEnabled;
        StatusMessage = settings.IsEnabled ? "AI 设置已加载。" : "AI 默认关闭。";
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        var saveApiKey = !string.IsNullOrWhiteSpace(ApiKey);
        await _settingsRepository.SaveAiSettingsAsync(
            new AiSettings(
                NormalizeOrDefault(Provider, AiSettings.DefaultProvider),
                NormalizeOrDefault(BaseUrl, AiSettings.DefaultBaseUrl),
                Model.Trim(),
                IsEnabled,
                saveApiKey ? ApiKey : null),
            saveApiKey,
            cancellationToken);

        if (saveApiKey)
        {
            HasSavedApiKey = true;
            ApiKey = string.Empty;
        }

        StatusMessage = "AI 设置已保存。";
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(Model))
        {
            StatusMessage = "请先启用 AI 并填写模型名称。";
            return false;
        }

        if (_classificationClient is null)
        {
            StatusMessage = "AI 客户端尚未接入。";
            return false;
        }

        try
        {
            var settings = await BuildCurrentSettingsForConnectionTestAsync(cancellationToken);
            await _classificationClient.SuggestAsync(settings, CreateConnectionTestRequest(), cancellationToken);
            StatusMessage = "AI 连接测试成功。";
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            StatusMessage = $"AI 连接测试失败：{ex.Message}";
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string NormalizeOrDefault(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static AiClassificationRequest CreateConnectionTestRequest()
    {
        return new AiClassificationRequest(
            "connection-test.txt",
            "MovieNavigator",
            "Connection test",
            null,
            null,
            [],
            null,
            null,
            null,
            MediaLibraryType.Normal);
    }

    private async Task<AiSettings> BuildCurrentSettingsForConnectionTestAsync(CancellationToken cancellationToken)
    {
        var saved = await _settingsRepository.LoadAiSettingsAsync(cancellationToken);
        return new AiSettings(
            NormalizeOrDefault(Provider, AiSettings.DefaultProvider),
            NormalizeOrDefault(BaseUrl, AiSettings.DefaultBaseUrl),
            Model.Trim(),
            IsEnabled,
            string.IsNullOrWhiteSpace(ApiKey) ? saved.ApiKey : ApiKey);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
