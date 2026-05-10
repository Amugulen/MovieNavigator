using FluentAssertions;
using MovieNavigator.App.Localization;
using MovieNavigator.App.ViewModels;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Tests.App;

public sealed class MainWindowViewModelTests
{
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

    private sealed class TemporaryVideoFolder : IDisposable
    {
        private const long LargeVideoSizeBytes = 101L * 1024L * 1024L;

        public TemporaryVideoFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"movie-navigator-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void CreateLargeVideo(string fileName)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            using var stream = File.Create(filePath);
            stream.SetLength(LargeVideoSizeBytes);
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
    }
}
