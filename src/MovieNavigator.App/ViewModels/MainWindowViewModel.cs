using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using MovieNavigator.App.Localization;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".ts", ".m2ts"
    };

    private const long MinimumQuickScanSizeBytes = 100L * 1024L * 1024L;

    private readonly IMediaRepository _mediaRepository;
    private string _searchText = string.Empty;
    private string _selectedSection = "首页";
    private string _statusMessage = "选择一个目录开始扫描。本版快速扫描会收录常见视频文件，默认过滤小于 100MB 的文件。";
    private string _resultSummary = "尚未扫描";

    public MainWindowViewModel(IAppLocalizer localizer, IMediaRepository mediaRepository)
    {
        _mediaRepository = mediaRepository;
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

    public ObservableCollection<string> DriveItems { get; } = [];

    public ObservableCollection<TagNodeViewModel> Tags { get; } =
    [
        new("country.soviet_union", "国家 / 苏联", []),
        new("genre.war", "类型 / 战争", []),
        new("decade.1970s", "年代 / 1970年代", []),
        new("storage.drive.d", "存储 / D盘", []),
        new("status.unconfirmed", "状态 / 待确认", [])
    ];

    public ObservableCollection<MediaCardViewModel> MediaCards { get; } = [];

    public ObservableCollection<PendingItemViewModel> PendingItems { get; } = [];

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public string ResultSummary
    {
        get => _resultSummary;
        private set => SetField(ref _resultSummary, value);
    }

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

    public async Task QuickScanFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        StatusMessage = $"正在扫描：{folderPath}";
        ResultSummary = "扫描中...";

        var files = EnumerateCandidateFiles(folderPath).ToList();
        var items = new List<MediaItem>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = CreateQuickScannedItem(file);
            await _mediaRepository.UpsertAsync(item, cancellationToken);
            items.Add(item);
        }

        RefreshFromItems(items);
        StatusMessage = $"扫描完成：发现 {items.Count} 个视频。快速扫描默认只收录大于 100MB 的常见视频文件。";
        ResultSummary = items.Count == 0 ? "没有符合收录规则的视频" : $"扫描结果：{items.Count} 个视频";
    }

    private static IEnumerable<FileInfo> EnumerateCandidateFiles(string folderPath)
    {
        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false
        };

        foreach (var path in Directory.EnumerateFiles(folderPath, "*.*", options))
        {
            FileInfo info;
            try
            {
                info = new FileInfo(path);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            if (VideoExtensions.Contains(info.Extension) && info.Length >= MinimumQuickScanSizeBytes)
            {
                yield return info;
            }
        }
    }

    private static MediaItem CreateQuickScannedItem(FileInfo file)
    {
        var now = DateTimeOffset.UtcNow;
        var driveKey = Path.GetPathRoot(file.FullName)?.TrimEnd('\\') ?? "unknown";
        var driveTag = CreateDriveTag(driveKey);

        return new MediaItem(
            Guid.NewGuid(),
            MediaLibraryType.Normal,
            MediaStatus.Pending,
            file.FullName,
            file.Name,
            driveKey,
            file.Length,
            TimeSpan.Zero,
            null,
            null,
            Path.GetFileNameWithoutExtension(file.Name),
            null,
            null,
            null,
            driveTag is null ? Array.Empty<TagKey>() : [driveTag.Value],
            now,
            now);
    }

    private static TagKey? CreateDriveTag(string driveKey)
    {
        var driveLetter = driveKey.TrimEnd(':').ToLowerInvariant();
        if (driveLetter.Length != 1 || driveLetter[0] < 'a' || driveLetter[0] > 'z')
        {
            return null;
        }

        return TagKey.Parse($"storage.drive.{driveLetter}");
    }

    private void RefreshFromItems(IReadOnlyCollection<MediaItem> items)
    {
        MediaCards.Clear();
        PendingItems.Clear();
        DriveItems.Clear();

        foreach (var item in items.OrderBy(item => item.FilePath))
        {
            MediaCards.Add(new MediaCardViewModel(
                item.Title ?? item.FileName,
                item.FilePath,
                item.Duration == TimeSpan.Zero ? "时长待分析" : item.Duration.ToString(@"hh\:mm\:ss"),
                item.Width is null || item.Height is null ? "分辨率待分析" : $"{item.Width}x{item.Height}",
                "待补充",
                item.DriveKey,
                item.Tags.Select(tag => tag.Value).ToArray()));

            PendingItems.Add(new PendingItemViewModel(item.FileName, item.FilePath, "快速扫描入库，等待补充资料或 ffprobe 分析"));
        }

        foreach (var group in items.GroupBy(item => item.DriveKey).OrderBy(group => group.Key))
        {
            DriveItems.Add($"{group.Key} {group.Count()}部");
        }
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
