namespace MovieNavigator.App.ViewModels;

public sealed record TagNodeViewModel(string Key, string DisplayName, IReadOnlyList<TagNodeViewModel> Children);
