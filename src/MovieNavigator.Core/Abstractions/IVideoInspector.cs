namespace MovieNavigator.Core.Abstractions;

public interface IVideoInspector
{
    Task<VideoInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken);
}

public sealed record VideoInspectionResult(TimeSpan Duration, int? Width, int? Height, string? Codec);
