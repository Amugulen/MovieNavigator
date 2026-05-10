namespace MovieNavigator.Core.Scanning;

public sealed class ScanRules
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".ts", ".m2ts"
    };

    private static readonly string[] ExcludedPathParts =
    [
        @"\cache\",
        @"\temp\",
        @"\tmp\",
        @"\node_modules\",
        @"\steamapps\",
        @"\game\",
        @"\games\"
    ];

    public IReadOnlyCollection<string> AuthorizedRoots { get; }
    public TimeSpan MinimumDuration { get; }
    public long MinimumSizeBytes { get; }

    private ScanRules(IReadOnlyCollection<string> authorizedRoots, TimeSpan minimumDuration, long minimumSizeBytes)
    {
        AuthorizedRoots = authorizedRoots;
        MinimumDuration = minimumDuration;
        MinimumSizeBytes = minimumSizeBytes;
    }

    public static ScanRules CreateDefault(IReadOnlyCollection<string> authorizedRoots)
    {
        return new ScanRules(
            authorizedRoots.Select(NormalizeRoot).ToArray(),
            TimeSpan.FromMinutes(20),
            100L * 1024L * 1024L);
    }

    public bool ShouldConsiderPath(string path)
    {
        var normalized = NormalizePath(path);
        if (!AuthorizedRoots.Any(root => normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (ExcludedPathParts.Any(part => normalized.Contains(part, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return VideoExtensions.Contains(Path.GetExtension(normalized));
    }

    public bool ShouldIncludeAnalyzedVideo(TimeSpan duration, long sizeBytes)
    {
        return duration >= MinimumDuration && sizeBytes >= MinimumSizeBytes;
    }

    private static string NormalizeRoot(string root)
    {
        var normalized = NormalizePath(root);
        return normalized.EndsWith('\\') ? normalized : normalized + "\\";
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('/', '\\').Trim();
    }
}
