using FluentAssertions;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Scanning;
using MovieNavigator.Tests.TestDoubles;

namespace MovieNavigator.Tests.Scanning;

public sealed class MediaScannerTests
{
    [Fact]
    public async Task ScanAsync_indexes_only_eligible_videos()
    {
        var fileSystem = new FakeFileSystem([
            new FakeFileEntry(@"D:\Movies\War\film.mkv", 2_000_000_000),
            new FakeFileEntry(@"D:\Movies\War\clip.mp4", 200_000_000),
            new FakeFileEntry(@"D:\Movies\War\notes.txt", 1_000)
        ]);
        var inspector = new FakeVideoInspector(new Dictionary<string, VideoInspectionResult>
        {
            [@"D:\Movies\War\film.mkv"] = new(TimeSpan.FromMinutes(125), 1920, 1080, "h264"),
            [@"D:\Movies\War\clip.mp4"] = new(TimeSpan.FromMinutes(4), 1280, 720, "h264")
        });
        var scanner = new MediaScanner(fileSystem, inspector);

        var result = await scanner.ScanAsync(ScanRules.CreateDefault([@"D:\Movies"]), MediaLibraryType.Normal, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].FilePath.Should().Be(@"D:\Movies\War\film.mkv");
        result[0].Status.Should().Be(MediaStatus.Pending);
        result[0].DriveKey.Should().Be("D:");
    }
}
