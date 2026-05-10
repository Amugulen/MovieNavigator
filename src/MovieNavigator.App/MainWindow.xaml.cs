using System.Diagnostics;
using System.IO;
using System.Windows;
using MovieNavigator.App.Localization;
using MovieNavigator.App.ViewModels;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(SqliteConnectionFactory databaseFactory)
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(CreateLocalizer(), new SqliteMediaRepository(databaseFactory));
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.LoadIndexAsync(CancellationToken.None);
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
