using FluentAssertions;
using MovieNavigator.App.Localization;
using MovieNavigator.App.ViewModels;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Indexing;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Tests.App;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task Load_index_populates_media_cards_from_repository_without_scan()
    {
        var repository = new InMemoryMediaRepository();
        var item = CreateMediaItem(
            @"D:\Movies\Soviet\film.mkv",
            "film.mkv",
            "Soviet Film",
            [MovieNavigator.Core.Tags.TagKey.Parse("country.soviet_union")]);
        await repository.UpsertAsync(item, CancellationToken.None);
        var viewModel = new MainWindowViewModel(new PassThroughLocalizer(), repository);

        await viewModel.LoadIndexAsync(CancellationToken.None);

        viewModel.MediaCards.Should().ContainSingle();
        viewModel.MediaCards[0].Title.Should().Be("Soviet Film");
        viewModel.DriveItems.Should().ContainSingle(drive => drive.Key == "D:");
        viewModel.ResultSummary.Should().Contain("1");
    }

    [Fact]
    public async Task Selecting_classification_facet_filters_media_cards()
    {
        var repository = new InMemoryMediaRepository();
        await repository.UpsertAsync(CreateMediaItem(
            @"D:\Movies\War\film.mkv",
            "film.mkv",
            "War Film",
            [MovieNavigator.Core.Tags.TagKey.Parse("genre.war")]), CancellationToken.None);
        await repository.UpsertAsync(CreateMediaItem(
            @"D:\Movies\Family\family.mp4",
            "family.mp4",
            "Family Film",
            []), CancellationToken.None);
        var viewModel = new MainWindowViewModel(new PassThroughLocalizer(), repository);

        await viewModel.LoadIndexAsync(CancellationToken.None);
        viewModel.ClassificationFacets.Should().Contain(facet => facet.Key == "type.mkv" && facet.Count == 1);

        viewModel.SelectedClassificationFacet = viewModel.ClassificationFacets.Single(facet => facet.Key == "type.mkv");

        viewModel.MediaCards.Should().ContainSingle();
        viewModel.MediaCards[0].Title.Should().Be("War Film");
        viewModel.ResultSummary.Should().Contain("1");
    }

    [Fact]
    public async Task Quick_scan_saves_root_and_incremental_scan_all_roots_reuses_it()
    {
        using var temp = new TemporaryVideoFolder();
        var original = temp.CreateLargeVideo("original.mkv");
        var mediaRepository = new InMemoryMediaRepository();
        var scanRootRepository = new InMemoryScanRootRepository();
        var viewModel = new MainWindowViewModel(new PassThroughLocalizer(), mediaRepository, scanRootRepository);

        await viewModel.QuickScanFolderAsync(temp.Path, CancellationToken.None);
        scanRootRepository.Roots.Should().ContainSingle(root => root.Path == temp.Path);

        File.Delete(original);
        temp.CreateLargeVideo("new-video.mp4");
        await viewModel.IncrementalScanAllRootsAsync(CancellationToken.None);

        viewModel.MediaCards.Select(card => card.Title).Should().Contain(["new-video", "original"]);
        (await mediaRepository.GetByPathAsync(original, CancellationToken.None))!.Status.Should().Be(MediaStatus.Offline);
    }

    [Fact]
    public async Task Search_text_filters_scanned_media_cards()
    {
        using var temp = new TemporaryVideoFolder();
        temp.CreateLargeVideo("soviet-war-film.mkv");
        temp.CreateLargeVideo("family-animation.mp4");
        var viewModel = new MainWindowViewModel(new PassThroughLocalizer(), new InMemoryMediaRepository());

        await viewModel.QuickScanFolderAsync(temp.Path, CancellationToken.None);
        viewModel.MediaCards.Should().HaveCount(2);

        viewModel.SearchText = "soviet";

        viewModel.MediaCards.Should().ContainSingle();
        viewModel.MediaCards[0].Title.Should().Be("soviet-war-film");
        viewModel.ResultSummary.Should().Contain("1");
    }

    [Fact]
    public async Task Selecting_tag_filters_scanned_media_cards()
    {
        using var temp = new TemporaryVideoFolder();
        temp.CreateLargeVideo("movie.mkv");
        var viewModel = new MainWindowViewModel(new PassThroughLocalizer(), new InMemoryMediaRepository());

        await viewModel.QuickScanFolderAsync(temp.Path, CancellationToken.None);
        viewModel.MediaCards.Should().ContainSingle();

        viewModel.SelectedTag = viewModel.Tags.Single(tag => tag.Key == "country.soviet_union");

        viewModel.MediaCards.Should().BeEmpty();
        viewModel.ResultSummary.Should().Contain("0");
    }

    [Fact]
    public async Task Selecting_normal_library_clears_active_filters()
    {
        using var temp = new TemporaryVideoFolder();
        temp.CreateLargeVideo("movie.mkv");
        var viewModel = new MainWindowViewModel(new PassThroughLocalizer(), new InMemoryMediaRepository());

        await viewModel.QuickScanFolderAsync(temp.Path, CancellationToken.None);
        viewModel.SelectedTag = viewModel.Tags.Single(tag => tag.Key == "country.soviet_union");
        viewModel.MediaCards.Should().BeEmpty();

        viewModel.SelectedSection = LocalizedStrings.NavNormalLibrary;

        viewModel.MediaCards.Should().ContainSingle();
        viewModel.SelectedTag.Should().BeNull();
        viewModel.ResultSummary.Should().Contain("扫描结果");
    }

    private static MediaItem CreateMediaItem(
        string filePath,
        string fileName,
        string title,
        IReadOnlyCollection<MovieNavigator.Core.Tags.TagKey> tags)
    {
        var now = DateTimeOffset.UtcNow;
        return new MediaItem(
            Guid.NewGuid(),
            MediaLibraryType.Normal,
            MediaStatus.Pending,
            filePath,
            fileName,
            Path.GetPathRoot(filePath)?.TrimEnd('\\') ?? "unknown",
            2_000_000_000,
            TimeSpan.FromMinutes(100),
            1920,
            1080,
            title,
            null,
            null,
            null,
            tags,
            now,
            now);
    }

    private sealed class TemporaryVideoFolder : IDisposable
    {
        private const long LargeVideoSizeBytes = 101L * 1024L * 1024L;

        public TemporaryVideoFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"movie-navigator-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateLargeVideo(string fileName)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            using var stream = File.Create(filePath);
            stream.SetLength(LargeVideoSizeBytes);
            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class PassThroughLocalizer : IAppLocalizer
    {
        public string CultureName => "test";

        public string Get(string key) => key;
    }

    private sealed class InMemoryMediaRepository : IMediaRepository
    {
        private readonly List<MediaItem> _items = [];

        public Task UpsertAsync(MediaItem item, CancellationToken cancellationToken)
        {
            _items.RemoveAll(existing => existing.FilePath == item.FilePath);
            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MediaItem>> GetAllAsync(
            MediaLibraryType libraryType,
            bool includeAdultWhenUnlocked,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<MediaItem>>(_items
                .Where(item => item.LibraryType == libraryType)
                .ToList());
        }

        public Task<MediaItem?> GetByPathAsync(string filePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(_items.SingleOrDefault(item => item.FilePath == filePath));
        }

        public Task<IReadOnlyList<MediaItem>> SearchAsync(
            string query,
            MediaLibraryType libraryType,
            bool includeAdultWhenUnlocked,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<MediaItem>>(_items);
        }

        public Task<IReadOnlyList<MediaItem>> GetByDriveAsync(
            string driveKey,
            MediaLibraryType libraryType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<MediaItem>>(_items.Where(item => item.DriveKey == driveKey).ToList());
        }

        public Task MarkMissingAsync(string filePath, DateTimeOffset missingSince, CancellationToken cancellationToken)
        {
            var item = _items.SingleOrDefault(existing => existing.FilePath == filePath);
            if (item is not null)
            {
                _items.Remove(item);
                _items.Add(item with { Status = MediaStatus.Offline, UpdatedAt = missingSince });
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryScanRootRepository : IScanRootRepository
    {
        public List<ScanRoot> Roots { get; } = [];

        public Task UpsertAsync(ScanRoot root, CancellationToken cancellationToken)
        {
            Roots.RemoveAll(existing => existing.Path == root.Path);
            Roots.Add(root);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ScanRoot>> GetEnabledAsync(MediaLibraryType libraryType, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ScanRoot>>(Roots
                .Where(root => root.LibraryType == libraryType && root.IsEnabled)
                .ToList());
        }

        public Task UpdateLastScanAtAsync(string path, DateTimeOffset scannedAt, CancellationToken cancellationToken)
        {
            var root = Roots.Single(existing => existing.Path == path);
            Roots.Remove(root);
            Roots.Add(root with { LastScanAt = scannedAt });
            return Task.CompletedTask;
        }
    }
}
