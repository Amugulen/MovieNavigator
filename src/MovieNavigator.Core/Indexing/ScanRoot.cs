using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.Indexing;

public sealed record ScanRoot(
    string Path,
    MediaLibraryType LibraryType,
    bool IsEnabled,
    DateTimeOffset? LastScanAt);
