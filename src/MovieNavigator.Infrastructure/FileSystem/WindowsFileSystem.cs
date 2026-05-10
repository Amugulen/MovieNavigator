using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Infrastructure.FileSystem;

public sealed class WindowsFileSystem : IFileSystem
{
    public async IAsyncEnumerable<FileSystemVideoCandidate> EnumerateFilesAsync(
        IReadOnlyCollection<string> roots,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileInfo info;
                try
                {
                    info = new FileInfo(file);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                await Task.Yield();
                yield return new FileSystemVideoCandidate(info.FullName, info.Length);
            }
        }
    }
}
