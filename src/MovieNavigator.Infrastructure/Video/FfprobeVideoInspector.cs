using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Infrastructure.Video;

public sealed class FfprobeVideoInspector : IVideoInspector
{
    private readonly string _ffprobePath;

    public FfprobeVideoInspector(string ffprobePath)
    {
        _ffprobePath = ffprobePath;
    }

    public async Task<VideoInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height,codec_name -show_entries format=duration -of json \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("ffprobe process did not start.");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            return new VideoInspectionResult(TimeSpan.Zero, null, null, null);
        }

        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        var format = root.GetProperty("format");
        var durationText = format.GetProperty("duration").GetString() ?? "0";
        var duration = TimeSpan.FromSeconds(double.Parse(durationText, CultureInfo.InvariantCulture));

        var streams = root.GetProperty("streams");
        if (streams.GetArrayLength() == 0)
        {
            return new VideoInspectionResult(duration, null, null, null);
        }

        var stream = streams[0];
        var width = stream.TryGetProperty("width", out var widthElement) ? widthElement.GetInt32() : (int?)null;
        var height = stream.TryGetProperty("height", out var heightElement) ? heightElement.GetInt32() : (int?)null;
        var codec = stream.TryGetProperty("codec_name", out var codecElement) ? codecElement.GetString() : null;
        return new VideoInspectionResult(duration, width, height, codec);
    }
}
