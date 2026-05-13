using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using MovieNavigator.App.Localization;
using MovieNavigator.App.ViewModels;
using MovieNavigator.Core.Ai;
using MovieNavigator.Infrastructure.Ai;
using MovieNavigator.Infrastructure.Persistence;
using MovieNavigator.Infrastructure.Video;

namespace MovieNavigator.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly AiSettingsViewModel _aiSettingsViewModel;
    private readonly SqliteAppSettingsRepository _aiSettingsRepository;
    private readonly OpenAiCompatibleClassificationClient _aiClassificationClient;

    public MainWindow(SqliteConnectionFactory databaseFactory)
    {
        InitializeComponent();
        _aiSettingsRepository = new SqliteAppSettingsRepository(databaseFactory);
        _aiClassificationClient = new OpenAiCompatibleClassificationClient(new HttpClient());
        _aiSettingsViewModel = new AiSettingsViewModel(
            _aiSettingsRepository,
            _aiClassificationClient);
        _viewModel = new MainWindowViewModel(
            CreateLocalizer(),
            new SqliteMediaRepository(databaseFactory),
            new SqliteScanRootRepository(databaseFactory),
            new FfmpegThumbnailGenerator("ffmpeg", CreateThumbnailDirectory()),
            new FfprobeVideoInspector("ffprobe"),
            _aiClassificationClient);
        DataContext = _viewModel;
        AiSettingsPanel.DataContext = _aiSettingsViewModel;
        Loaded += async (_, _) =>
        {
            await _viewModel.LoadIndexAsync(CancellationToken.None);
            await _aiSettingsViewModel.LoadAsync(CancellationToken.None);
        };
    }

    private static string CreateThumbnailDirectory()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MovieNavigator");
        return Path.Combine(appData, "thumbnails");
    }

    private static IAppLocalizer CreateLocalizer()
    {
        return JsonAppLocalizer.FromDictionaries(
            "zh-CN",
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["zh-CN"] = new Dictionary<string, string>
                {
                    [LocalizedStrings.AppTitle] = "本地影视资料库",
                    [LocalizedStrings.NavHome] = "首页",
                    [LocalizedStrings.NavNormalLibrary] = "普通库",
                    [LocalizedStrings.NavDriveBrowse] = "按硬盘浏览",
                    [LocalizedStrings.NavTagIndex] = "TAG索引",
                    [LocalizedStrings.NavPending] = "待确认",
                    [LocalizedStrings.NavSettings] = "设置",
                    [LocalizedStrings.NavAdultLocked] = "成人库（锁定）",
                    [LocalizedStrings.DetailTitle] = "影片详情",
                    [LocalizedStrings.ActionOpenDefaultPlayer] = "默认播放器打开",
                    [LocalizedStrings.ActionOpenFolder] = "打开所在目录",
                    [LocalizedStrings.ActionMoveOrganize] = "移动/整理",
                    [LocalizedStrings.ActionRescan] = "重新识别",
                    [LocalizedStrings.ActionAddTag] = "添加TAG",
                    [LocalizedStrings.PendingWorkbench] = "待确认工作台"
                },
                ["en-US"] = new Dictionary<string, string>()
            });
    }

    private async void ScanFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择要扫描的影视目录",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        await _viewModel.QuickScanFolderAsync(dialog.SelectedPath, CancellationToken.None);
    }

    private async void IncrementalScanAllRootsButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.IncrementalScanAllRootsAsync(CancellationToken.None);
    }

    private async void SaveAiSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        await _aiSettingsViewModel.SaveAsync(CancellationToken.None);
    }

    private async void TestAiConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        await _aiSettingsViewModel.TestConnectionAsync(CancellationToken.None);
    }

    private void AiApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _aiSettingsViewModel.ApiKey = AiApiKeyBox.Password;
    }

    private async void SuggestTagsWithAiButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _viewModel.SelectedMedia;
        if (selected is null)
        {
            ShowInfo("请先在中间列表中选择一个影片。");
            return;
        }

        var request = BuildAiRequest(selected);
        var message = "将只发送以下文本字段给 AI，不发送截图、视频、音频：\n\n" +
            string.Join(Environment.NewLine, request.ToConfirmationLines());
        var confirm = System.Windows.MessageBox.Show(
            message,
            "用AI根据文本线索建议TAG",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        try
        {
            var settings = await _aiSettingsRepository.LoadAiSettingsAsync(CancellationToken.None);
            var suggestion = await _aiClassificationClient.SuggestAsync(settings, request, CancellationToken.None);
            var suggestedTags = suggestion.Tags.Select(tag => tag.Value).ToArray();
            var apply = System.Windows.MessageBox.Show(
                $"AI 建议：\n标题：{suggestion.Title ?? "-"}\nTAG：{string.Join(", ", suggestedTags)}\n置信度：{suggestion.Confidence:0.00}\n\n确认后只会写入这些 TAG，不会自动移动文件或改其他资料。",
                "确认写入 AI 建议 TAG",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);
            if (apply != MessageBoxResult.OK)
            {
                ShowInfo("已取消写入，媒体数据未修改。");
                return;
            }

            await _viewModel.ConfirmAiSuggestionAsync(suggestion, CancellationToken.None);
            ShowInfo($"已写入 TAG：{string.Join(", ", suggestedTags)}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
        {
            ShowInfo($"AI 建议失败，数据未修改：{ex.Message}");
        }
    }

    private void ThumbnailGridButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetThumbnailGridView();
    }

    private void CompactListButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetCompactListView();
    }

    private void DetailListButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetDetailListView();
    }

    private void OpenDefaultPlayerButton_Click(object sender, RoutedEventArgs e)
    {
        var path = GetSelectedExistingFilePath();
        if (path is null)
        {
            return;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var path = GetSelectedExistingFilePath();
        if (path is null)
        {
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
    }

    private void MoveOrganizeButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInfo("移动/整理功能还没有接入真实文件操作。当前版本只支持扫描、选中、默认播放器打开、打开所在目录。");
    }

    private void RescanButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInfo("重新识别功能还没有接入 ffprobe/TMDb/AI。当前版本使用快速扫描入库，时长和分辨率会显示为待分析。");
    }

    private void AddTagButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInfo("添加 TAG 功能还没有接入编辑器。当前版本会自动添加硬盘 TAG，例如 storage.drive.d。");
    }

    private static AiClassificationRequest BuildAiRequest(MediaCardViewModel selected)
    {
        var directory = Path.GetDirectoryName(selected.FilePath) ?? string.Empty;
        var resolutionParts = selected.Resolution.Split('x', StringSplitOptions.TrimEntries);
        int? width = resolutionParts.Length == 2 && int.TryParse(resolutionParts[0], out var parsedWidth)
            ? parsedWidth
            : null;
        int? height = resolutionParts.Length == 2 && int.TryParse(resolutionParts[1], out var parsedHeight)
            ? parsedHeight
            : null;
        var duration = TimeSpan.TryParse(selected.Duration, out var parsedDuration) ? parsedDuration : (TimeSpan?)null;

        return new AiClassificationRequest(
            Path.GetFileName(selected.FilePath),
            directory,
            selected.Title,
            null,
            null,
            selected.Tags.Select(MovieNavigator.Core.Tags.TagKey.Parse).ToArray(),
            duration,
            width,
            height,
            MovieNavigator.Core.Media.MediaLibraryType.Normal);
    }

    private string? GetSelectedExistingFilePath()
    {
        var selected = _viewModel.SelectedMedia;
        if (selected is null)
        {
            ShowInfo("请先在中间列表中选择一个影片。");
            return null;
        }

        if (!File.Exists(selected.FilePath))
        {
            ShowInfo($"文件不存在或硬盘未连接：{selected.FilePath}");
            return null;
        }

        return selected.FilePath;
    }

    private static void ShowInfo(string message)
    {
        System.Windows.MessageBox.Show(message, "Movie Navigator", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
