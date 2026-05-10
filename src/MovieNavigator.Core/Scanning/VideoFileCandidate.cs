namespace MovieNavigator.Core.Scanning;

public sealed record VideoFileCandidate(
    string FilePath,
    long SizeBytes,
    TimeSpan Duration,
    int? Width,
    int? Height,
    string? Codec);
