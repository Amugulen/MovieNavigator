using MovieNavigator.Core.Ai;

namespace MovieNavigator.Core.Abstractions;

public interface IAiSettingsRepository
{
    Task<AiSettings> LoadAiSettingsAsync(CancellationToken cancellationToken);

    Task SaveAiSettingsAsync(
        AiSettings settings,
        bool saveApiKey,
        CancellationToken cancellationToken);
}
