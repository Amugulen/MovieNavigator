namespace MovieNavigator.Core.Abstractions;

public interface IThumbnailGenerator
{
    Task<string?> GenerateAsync(string videoPath, CancellationToken cancellationToken);
}
