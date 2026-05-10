namespace MovieNavigator.App.ViewModels;

public sealed record MediaCardViewModel(
    string Title,
    string FilePath,
    string Duration,
    string Resolution,
    string Status,
    string DriveKey,
    IReadOnlyList<string> Tags);
