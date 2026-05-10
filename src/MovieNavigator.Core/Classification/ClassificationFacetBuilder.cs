using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.Classification;

public static class ClassificationFacetBuilder
{
    public static IReadOnlyList<ClassificationFacet> Build(IEnumerable<MediaItem> items)
    {
        var counts = new Dictionary<string, (string DisplayName, string Group, int Count)>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            foreach (var facet in BuildKeys(item))
            {
                if (counts.TryGetValue(facet.Key, out var existing))
                {
                    counts[facet.Key] = (existing.DisplayName, existing.Group, existing.Count + 1);
                }
                else
                {
                    counts[facet.Key] = (facet.DisplayName, facet.Group, 1);
                }
            }
        }

        return counts
            .Select(pair => new ClassificationFacet(pair.Key, pair.Value.DisplayName, pair.Value.Group, pair.Value.Count))
            .OrderBy(facet => facet.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(facet => facet.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<ClassificationFacet> BuildKeys(MediaItem item)
    {
        var facets = new List<ClassificationFacet>();
        var driveLetter = item.DriveKey.TrimEnd(':').ToLowerInvariant();
        if (driveLetter.Length == 1 && driveLetter[0] >= 'a' && driveLetter[0] <= 'z')
        {
            facets.Add(new ClassificationFacet($"storage.drive.{driveLetter}", $"硬盘 / {item.DriveKey}", "硬盘", 1));
        }

        facets.Add(new ClassificationFacet($"status.{item.Status.ToString().ToLowerInvariant()}", $"状态 / {DisplayStatus(item.Status)}", "状态", 1));

        var extension = Path.GetExtension(item.FileName).TrimStart('.').ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(extension))
        {
            facets.Add(new ClassificationFacet($"type.{extension}", $"类型 / {extension.ToUpperInvariant()}", "类型", 1));
        }

        if (item.Year is not null)
        {
            var decadeStart = item.Year.Value / 10 * 10;
            facets.Add(new ClassificationFacet($"decade.{decadeStart}s", $"年代 / {decadeStart}s", "年代", 1));
        }

        if (item.Height is not null)
        {
            var resolutionKey = BuildResolutionKey(item.Height.Value);
            facets.Add(new ClassificationFacet(resolutionKey, $"清晰度 / {resolutionKey.Split('.')[1]}", "清晰度", 1));
        }

        var durationKey = BuildDurationKey(item.Duration);
        facets.Add(new ClassificationFacet(durationKey, $"时长 / {durationKey.Split('.')[1]}", "时长", 1));

        facets.AddRange(item.Tags.Select(tag => new ClassificationFacet(tag.Value, $"TAG / {tag.Value}", "TAG", 1)));
        return facets;
    }

    private static string BuildResolutionKey(int height)
    {
        if (height >= 2160)
        {
            return "resolution.4k";
        }

        if (height >= 1080)
        {
            return "resolution.1080p";
        }

        if (height >= 720)
        {
            return "resolution.720p";
        }

        return "resolution.sd";
    }

    private static string BuildDurationKey(TimeSpan duration)
    {
        if (duration >= TimeSpan.FromMinutes(120))
        {
            return "duration.long";
        }

        if (duration >= TimeSpan.FromMinutes(45))
        {
            return "duration.feature";
        }

        return "duration.short";
    }

    private static string DisplayStatus(MediaStatus status)
    {
        return status switch
        {
            MediaStatus.Pending => "待确认",
            MediaStatus.Confirmed => "已确认",
            MediaStatus.Ignored => "已忽略",
            MediaStatus.Offline => "离线",
            _ => status.ToString()
        };
    }
}
