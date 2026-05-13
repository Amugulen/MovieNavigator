using MovieNavigator.Core.Ai;

namespace MovieNavigator.Core.Abstractions;

public interface IAiClassificationClient
{
    Task<AiClassificationSuggestion> SuggestAsync(
        AiSettings settings,
        AiClassificationRequest request,
        CancellationToken cancellationToken);
}
