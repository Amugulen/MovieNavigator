namespace MovieNavigator.Core.Tags;

public sealed record TagDefinition(
    TagKey Key,
    string DisplayNameZh,
    string DisplayNameEn,
    IReadOnlyCollection<string> Aliases,
    TagKey? ParentKey);
