using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MovieNavigator.App.Localization;

namespace MovieNavigator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _searchText = string.Empty;
    private string _selectedSection = "首页";

    public MainWindowViewModel(IAppLocalizer localizer)
    {
        AppTitle = localizer.Get(LocalizedStrings.AppTitle);
        DetailTitle = localizer.Get(LocalizedStrings.DetailTitle);
        PendingWorkbenchTitle = localizer.Get(LocalizedStrings.PendingWorkbench);
        OpenDefaultPlayerText = localizer.Get(LocalizedStrings.ActionOpenDefaultPlayer);
        OpenFolderText = localizer.Get(LocalizedStrings.ActionOpenFolder);
        MoveOrganizeText = localizer.Get(LocalizedStrings.ActionMoveOrganize);
        RescanText = localizer.Get(LocalizedStrings.ActionRescan);
        AddTagText = localizer.Get(LocalizedStrings.ActionAddTag);

        NavigationItems =
        [
            localizer.Get(LocalizedStrings.NavHome),
            localizer.Get(LocalizedStrings.NavNormalLibrary),
            localizer.Get(LocalizedStrings.NavDriveBrowse),
            localizer.Get(LocalizedStrings.NavTagIndex),
            localizer.Get(LocalizedStrings.NavPending),
            localizer.Get(LocalizedStrings.NavSettings),
            localizer.Get(LocalizedStrings.NavAdultLocked)
        ];
    }

    public string AppTitle { get; }
    public string DetailTitle { get; }
    public string PendingWorkbenchTitle { get; }
    public string OpenDefaultPlayerText { get; }
    public string OpenFolderText { get; }
    public string MoveOrganizeText { get; }
    public string RescanText { get; }
    public string AddTagText { get; }

    public ObservableCollection<string> NavigationItems { get; }

    public ObservableCollection<string> DriveItems { get; } =
    [
        "D盘 0部",
        "E盘 0部",
        "移动硬盘A 离线 0部"
    ];

    public ObservableCollection<TagNodeViewModel> Tags { get; } =
    [
        new("country.soviet_union", "国家 / 苏联", []),
        new("genre.war", "类型 / 战争", []),
        new("decade.1970s", "年代 / 1970年代", []),
        new("storage.drive.d", "存储 / D盘", []),
        new("status.unconfirmed", "状态 / 待确认", [])
    ];

    public ObservableCollection<MediaCardViewModel> MediaCards { get; } =
    [
        new("未识别影片", @"D:\Movies\example.mkv", "02:14:36", "1080p", "待补充", "D:", ["country.soviet_union", "genre.war"])
    ];

    public ObservableCollection<PendingItemViewModel> PendingItems { get; } =
    [
        new("example.mkv", @"D:\Movies\example.mkv", "填写标题 / 导演 / 介绍网址后可重新识别")
    ];

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public string SelectedSection
    {
        get => _selectedSection;
        set => SetField(ref _selectedSection, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
