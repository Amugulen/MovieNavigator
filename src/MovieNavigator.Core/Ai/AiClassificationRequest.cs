using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Core.Ai;

public sealed record AiClassificationRequest(
    string FileName,
    string FolderPath,
    string? ManualTitle,
    string? ManualIdentifier,
    string? ManualUrl,
    IReadOnlyCollection<TagKey> ExistingTags,
    TimeSpan? Duration,
    int? Width,
    int? Height,
    MediaLibraryType LibraryType)
{
    public IReadOnlyList<string> ToConfirmationLines()
    {
        return
        [
            $"文件名: {FileName}",
            $"文件夹路径: {FolderPath}",
            $"手动标题: {Display(ManualTitle)}",
            $"手动标识/番号: {Display(ManualIdentifier)}",
            $"手动网址: {Display(ManualUrl)}",
            $"已有TAG: {(ExistingTags.Count == 0 ? "-" : string.Join(", ", ExistingTags.Select(tag => tag.Value)))}",
            $"时长: {(Duration is null || Duration == TimeSpan.Zero ? "-" : Duration.Value.ToString(@"hh\:mm\:ss"))}",
            $"分辨率: {(Width is null || Height is null ? "-" : $"{Width}x{Height}")}",
            $"库类型: {LibraryType}"
        ];
    }

    public TextOnlyAiClassificationPayload ToTextOnlyPayload()
    {
        return new TextOnlyAiClassificationPayload(
            FileName,
            FolderPath,
            ManualTitle,
            ManualIdentifier,
            ManualUrl,
            ExistingTags.Select(tag => tag.Value).ToArray(),
            Duration is null || Duration == TimeSpan.Zero ? null : Duration.Value.ToString(@"hh\:mm\:ss"),
            Width is null || Height is null ? null : $"{Width}x{Height}",
            LibraryType.ToString());
    }

    private static string Display(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}

public sealed record TextOnlyAiClassificationPayload(
    string FileName,
    string FolderPath,
    string? ManualTitle,
    string? ManualIdentifier,
    string? ManualUrl,
    IReadOnlyCollection<string> ExistingTags,
    string? Duration,
    string? Resolution,
    string LibraryType);
