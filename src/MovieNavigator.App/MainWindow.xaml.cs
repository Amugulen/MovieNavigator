using System.Windows;
using MovieNavigator.App.Localization;
using MovieNavigator.App.ViewModels;

namespace MovieNavigator.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(CreateLocalizer());
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
}
