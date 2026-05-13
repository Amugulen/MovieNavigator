using MovieNavigator.Core.Tags;

namespace MovieNavigator.Core.Ai;

public sealed record AiClassificationSuggestion(
    string? Title,
    int? Year,
    string? Summary,
    IReadOnlyCollection<TagKey> Tags,
    double Confidence,
    string? Notes);
