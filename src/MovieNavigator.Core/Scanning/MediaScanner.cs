using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.Scanning;

public sealed class MediaScanner
{
    private readonly IFileSystem _fileSystem;
    private readonly IVideoInspector _videoInspector;

    public MediaScanner(IFileSystem fileSystem, IVideoInspector videoInspector)
    {
        _fileSystem = fileSystem;
        _videoInspector = videoInspector;
    }

    public async Task<IReadOnlyList<MediaItem>> ScanAsync(ScanRules rules, MediaLibraryType libraryType, CancellationToken cancellationToken)
    {
        var items = new List<MediaItem>();

        await foreach (var file in _fileSystem.EnumerateFilesAsync(rules.AuthorizedRoots, cancellationToken))
        {
            if (!rules.ShouldConsiderPath(file.FullPath))
            {
                continue;
            }

            var inspection = await _videoInspector.InspectAsync(file.FullPath, cancellationToken);
            if (!rules.ShouldIncludeAnalyzedVideo(inspection.Duration, file.SizeBytes))
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            items.Add(new MediaItem(
                Guid.NewGuid(),
                libraryType,
                MediaStatus.Pending,
                file.FullPath,
                Path.GetFileName(file.FullPath),
                GetDriveKey(file.FullPath),
                file.SizeBytes,
                inspection.Duration,
                inspection.Width,
                inspection.Height,
                null,
                null,
                null,
                null,
                Array.Empty<Tags.TagKey>(),
                now,
                now));
        }

        return items;
    }

    private static string GetDriveKey(string path)
    {
        var root = Path.GetPathRoot(path);
        return string.IsNullOrWhiteSpace(root) ? "unknown" : root.TrimEnd('\\');
    }
}
