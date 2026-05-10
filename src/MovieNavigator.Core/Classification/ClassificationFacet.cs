namespace MovieNavigator.Core.Classification;

public sealed record ClassificationFacet(
    string Key,
    string DisplayName,
    string Group,
    int Count);
