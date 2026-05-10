using FluentAssertions;
using MovieNavigator.Infrastructure.Video;

namespace MovieNavigator.Tests.Video;

public sealed class ThumbnailGeneratorTests
{
    [Fact]
    public async Task Generate_async_returns_null_when_ffmpeg_is_missing()
    {
        using var temp = new TemporaryFolder();
        var input = Path.Combine(temp.Path, "sample.mp4");
        await File.WriteAllTextAsync(input, "not a real video");
        var generator = new FfmpegThumbnailGenerator(
            ffmpegPath: Path.Combine(temp.Path, "missing-ffmpeg.exe"),
            thumbnailDirectory: Path.Combine(temp.Path, "thumbnails"));

        var thumbnail = await generator.GenerateAsync(input, CancellationToken.None);

        thumbnail.Should().BeNull();
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"movie-navigator-thumbnail-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
