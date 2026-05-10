using MovieNavigator.Core.Tags;

namespace MovieNavigator.Core.Media;

public sealed record MediaItem(
    Guid Id,
    MediaLibraryType LibraryType,
    MediaStatus Status,
    string FilePath,
    string FileName,
    string DriveKey,
    long SizeBytes,
    TimeSpan Duration,
    int? Width,
    int? Height,
    string? Title,
    string? OriginalTitle,
    int? Year,
    string? Summary,
    IReadOnlyCollection<TagKey> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
