namespace MovieNavigator.App.ViewModels;

public sealed record MediaCardViewModel(
    string Title,
    string FilePath,
    string Duration,
    string Resolution,
    string Status,
    string DriveKey,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> ClassificationKeys,
    string? ThumbnailPath = null,
    string? Extension = null,
    bool IsMissing = false);
