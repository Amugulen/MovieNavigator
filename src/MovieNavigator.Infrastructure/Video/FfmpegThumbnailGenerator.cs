using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Infrastructure.Video;

public sealed class FfmpegThumbnailGenerator : IThumbnailGenerator
{
    private readonly string _ffmpegPath;
    private readonly string _thumbnailDirectory;

    public FfmpegThumbnailGenerator(string ffmpegPath, string thumbnailDirectory)
    {
        _ffmpegPath = ffmpegPath;
        _thumbnailDirectory = thumbnailDirectory;
    }

    public async Task<string?> GenerateAsync(string videoPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_thumbnailDirectory);
        var outputPath = Path.Combine(_thumbnailDirectory, $"{CreateStableName(videoPath)}.jpg");
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = $"-y -ss 00:00:10 -i \"{videoPath}\" -frames:v 1 -vf scale=360:-1 \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0 && File.Exists(outputPath) ? outputPath : null;
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string CreateStableName(string videoPath)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(videoPath.ToUpperInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
