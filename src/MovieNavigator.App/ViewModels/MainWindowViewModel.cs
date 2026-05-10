using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using MovieNavigator.App.Localization;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Indexing;
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
    private readonly IScanRootRepository? _scanRootRepository;
    private readonly string _homeSection;
    private readonly string _normalLibrarySection;
    private readonly string _driveBrowseSection;
    private readonly string _tagIndexSection;
    private readonly string _pendingSection;
    private readonly string _settingsSection;
    private readonly string _adultLockedSection;
    private string _searchText = string.Empty;
    private string _selectedSection = string.Empty;
    private string _statusMessage = "选择一个目录开始扫描。本版快速扫描会收录常见视频文件，默认过滤小于 100MB 的文件。";
    private string _resultSummary = "尚未扫描";
    private MediaCardViewModel? _selectedMedia;
    private DriveItemViewModel? _selectedDrive;
    private TagNodeViewModel? _selectedTag;
    private readonly List<MediaCardViewModel> _allMediaCards = [];

    public MainWindowViewModel(IAppLocalizer localizer, IMediaRepository mediaRepository, IScanRootRepository? scanRootRepository = null)
    {
        _mediaRepository = mediaRepository;
        _scanRootRepository = scanRootRepository;
        AppTitle = localizer.Get(LocalizedStrings.AppTitle);
        DetailTitle = localizer.Get(LocalizedStrings.DetailTitle);
        PendingWorkbenchTitle = localizer.Get(LocalizedStrings.PendingWorkbench);
        OpenDefaultPlayerText = localizer.Get(LocalizedStrings.ActionOpenDefaultPlayer);
        OpenFolderText = localizer.Get(LocalizedStrings.ActionOpenFolder);
        MoveOrganizeText = localizer.Get(LocalizedStrings.ActionMoveOrganize);
        RescanText = localizer.Get(LocalizedStrings.ActionRescan);
        AddTagText = localizer.Get(LocalizedStrings.ActionAddTag);
        _homeSection = localizer.Get(LocalizedStrings.NavHome);
        _normalLibrarySection = localizer.Get(LocalizedStrings.NavNormalLibrary);
        _driveBrowseSection = localizer.Get(LocalizedStrings.NavDriveBrowse);
        _tagIndexSection = localizer.Get(LocalizedStrings.NavTagIndex);
        _pendingSection = localizer.Get(LocalizedStrings.NavPending);
        _settingsSection = localizer.Get(LocalizedStrings.NavSettings);
        _adultLockedSection = localizer.Get(LocalizedStrings.NavAdultLocked);
        _selectedSection = _homeSection;

        NavigationItems =
        [
            _homeSection,
            _normalLibrarySection,
            _driveBrowseSection,
            _tagIndexSection,
            _pendingSection,
            _settingsSection,
            _adultLockedSection
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

    public ObservableCollection<DriveItemViewModel> DriveItems { get; } = [];

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

    public MediaCardViewModel? SelectedMedia
    {
        get => _selectedMedia;
        set
        {
            if (EqualityComparer<MediaCardViewModel?>.Default.Equals(_selectedMedia, value))
            {
                return;
            }

            _selectedMedia = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedMedia)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTitle)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPath)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDuration)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedResolution)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTags)));
        }
    }

    public string SelectedTitle => SelectedMedia?.Title ?? "未选择影片";
    public string SelectedPath => SelectedMedia?.FilePath ?? "请先在列表中选择一个影片";
    public string SelectedDuration => SelectedMedia?.Duration ?? "-";
    public string SelectedResolution => SelectedMedia?.Resolution ?? "-";
    public string SelectedTags => SelectedMedia is null ? "-" : string.Join(", ", SelectedMedia.Tags);

    public DriveItemViewModel? SelectedDrive
    {
        get => _selectedDrive;
        set
        {
            if (EqualityComparer<DriveItemViewModel?>.Default.Equals(_selectedDrive, value))
            {
                return;
            }

            _selectedDrive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDrive)));
            ApplyFilters();
        }
    }

    public TagNodeViewModel? SelectedTag
    {
        get => _selectedTag;
        set
        {
            if (EqualityComparer<TagNodeViewModel?>.Default.Equals(_selectedTag, value))
            {
                return;
            }

            _selectedTag = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTag)));
            ApplyFilters();
        }
    }

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
        set
        {
            if (EqualityComparer<string>.Default.Equals(_searchText, value))
            {
                return;
            }

            _searchText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
            ApplyFilters();
        }
    }

    public string SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (EqualityComparer<string>.Default.Equals(_selectedSection, value))
            {
                return;
            }

            _selectedSection = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSection)));
            HandleSelectedSectionChanged(value);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoadIndexAsync(CancellationToken cancellationToken)
    {
        var items = await _mediaRepository.GetAllAsync(MediaLibraryType.Normal, includeAdultWhenUnlocked: false, cancellationToken);
        RefreshFromItems(items);

        StatusMessage = items.Count == 0
            ? "索引为空。请选择目录进行首次扫描。"
            : $"已从索引加载 {items.Count} 个视频。";
        ResultSummary = items.Count == 0 ? "索引为空" : $"索引中已有 {items.Count} 个视频";
    }

    public async Task QuickScanFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        StatusMessage = $"正在扫描：{folderPath}";
        ResultSummary = "扫描中...";

        await SaveScanRootAsync(folderPath, cancellationToken);
        var items = await ScanSingleRootAsync(folderPath, markMissing: false, cancellationToken);

        RefreshFromItems(items);
        StatusMessage = $"扫描完成：发现 {items.Count} 个视频。快速扫描默认只收录大于 100MB 的常见视频文件。";
        ResultSummary = items.Count == 0 ? "没有符合收录规则的视频" : $"扫描结果：{items.Count} 个视频";
    }

    public async Task IncrementalScanAllRootsAsync(CancellationToken cancellationToken)
    {
        if (_scanRootRepository is null)
        {
            StatusMessage = "当前版本未接入扫描目录存储，无法执行增量扫描。";
            return;
        }

        var roots = await _scanRootRepository.GetEnabledAsync(MediaLibraryType.Normal, cancellationToken);
        if (roots.Count == 0)
        {
            StatusMessage = "还没有保存扫描目录。请先选择目录并扫描一次。";
            ResultSummary = "没有扫描目录";
            return;
        }

        var scannedCount = 0;
        foreach (var root in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(root.Path))
            {
                await MarkIndexedFilesUnderMissingRootAsync(root.Path, cancellationToken);
                continue;
            }

            scannedCount += (await ScanSingleRootAsync(root.Path, markMissing: true, cancellationToken)).Count;
        }

        await LoadIndexAsync(cancellationToken);
        StatusMessage = $"增量扫描完成：检查 {roots.Count} 个目录，发现 {scannedCount} 个当前可访问视频。";
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

    private async Task SaveScanRootAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (_scanRootRepository is null)
        {
            return;
        }

        var root = new ScanRoot(folderPath, MediaLibraryType.Normal, IsEnabled: true, LastScanAt: DateTimeOffset.UtcNow);
        await _scanRootRepository.UpsertAsync(root, cancellationToken);
    }

    private async Task<IReadOnlyList<MediaItem>> ScanSingleRootAsync(string folderPath, bool markMissing, CancellationToken cancellationToken)
    {
        var files = EnumerateCandidateFiles(folderPath).ToList();
        var items = new List<MediaItem>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = CreateQuickScannedItem(file);
            await _mediaRepository.UpsertAsync(item, cancellationToken);
            items.Add(item);
            seenPaths.Add(item.FilePath);
        }

        if (markMissing)
        {
            var indexed = await _mediaRepository.GetAllAsync(MediaLibraryType.Normal, includeAdultWhenUnlocked: false, cancellationToken);
            foreach (var item in indexed.Where(item => IsUnderRoot(item.FilePath, folderPath) && !seenPaths.Contains(item.FilePath)))
            {
                await _mediaRepository.MarkMissingAsync(item.FilePath, DateTimeOffset.UtcNow, cancellationToken);
            }
        }

        if (_scanRootRepository is not null)
        {
            await _scanRootRepository.UpdateLastScanAtAsync(folderPath, DateTimeOffset.UtcNow, cancellationToken);
        }

        return items;
    }

    private async Task MarkIndexedFilesUnderMissingRootAsync(string folderPath, CancellationToken cancellationToken)
    {
        var indexed = await _mediaRepository.GetAllAsync(MediaLibraryType.Normal, includeAdultWhenUnlocked: false, cancellationToken);
        foreach (var item in indexed.Where(item => IsUnderRoot(item.FilePath, folderPath)))
        {
            await _mediaRepository.MarkMissingAsync(item.FilePath, DateTimeOffset.UtcNow, cancellationToken);
        }
    }

    private static bool IsUnderRoot(string filePath, string folderPath)
    {
        var normalizedFile = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedFolder = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return normalizedFile.StartsWith(normalizedFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedFile, normalizedFolder, StringComparison.OrdinalIgnoreCase);
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
        _allMediaCards.Clear();
        MediaCards.Clear();
        PendingItems.Clear();
        DriveItems.Clear();

        foreach (var item in items.OrderBy(item => item.FilePath))
        {
            _allMediaCards.Add(new MediaCardViewModel(
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
            DriveItems.Add(new DriveItemViewModel(group.Key, $"{group.Key} {group.Count()}部"));
        }

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var query = _searchText.Trim();
        var selectedDriveKey = SelectedDrive?.Key;
        var selectedTagKey = SelectedTag?.Key;

        var filtered = _allMediaCards.Where(card =>
            MatchesSearch(card, query) &&
            (selectedDriveKey is null || string.Equals(card.DriveKey, selectedDriveKey, StringComparison.OrdinalIgnoreCase)) &&
            (selectedTagKey is null || card.Tags.Any(tag => string.Equals(tag, selectedTagKey, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        MediaCards.Clear();
        foreach (var card in filtered)
        {
            MediaCards.Add(card);
        }

        SelectedMedia = MediaCards.FirstOrDefault();

        if (_allMediaCards.Count == 0)
        {
            return;
        }

        if (query.Length > 0 || selectedDriveKey is not null || selectedTagKey is not null)
        {
            ResultSummary = $"筛选结果：{filtered.Count} / {_allMediaCards.Count} 个视频";
            return;
        }

        ResultSummary = $"扫描结果：{_allMediaCards.Count} 个视频";
    }

    private static bool MatchesSearch(MediaCardViewModel card, string query)
    {
        if (query.Length == 0)
        {
            return true;
        }

        return Contains(card.Title, query) ||
            Contains(card.FilePath, query) ||
            Contains(card.DriveKey, query) ||
            card.Tags.Any(tag => Contains(tag, query));
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void HandleSelectedSectionChanged(string section)
    {
        if (section == _homeSection || section == _normalLibrarySection)
        {
            ClearFilters();
            StatusMessage = "已显示普通库全部扫描结果。";
            return;
        }

        if (section == _driveBrowseSection)
        {
            StatusMessage = "在左侧硬盘列表选择一个硬盘后，中间列表会只显示该硬盘的视频。";
            return;
        }

        if (section == _tagIndexSection)
        {
            StatusMessage = "在左侧 TAG 索引选择一个 TAG 后，中间列表会只显示命中该 TAG 的视频。";
            return;
        }

        if (section == _pendingSection)
        {
            ClearFilters();
            StatusMessage = "待确认工作台会列出快速扫描后还需要补充资料的视频。";
            return;
        }

        if (section == _settingsSection)
        {
            StatusMessage = "设置页还没有展开；后续会放 API Key、语言、扫描规则和成人库开关。";
            return;
        }

        if (section == _adultLockedSection)
        {
            ClearFilters();
            StatusMessage = "成人库当前锁定；后续必须通过授权解锁后才会显示成人内容。";
        }
    }

    private void ClearFilters()
    {
        _searchText = string.Empty;
        _selectedDrive = null;
        _selectedTag = null;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDrive)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTag)));
        ApplyFilters();
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
