namespace MovieNavigator.App.ViewModels;

public sealed record ClassificationFacetViewModel(
    string Key,
    string DisplayName,
    string Group,
    int Count)
{
    public string Label => $"{DisplayName} ({Count})";
}
