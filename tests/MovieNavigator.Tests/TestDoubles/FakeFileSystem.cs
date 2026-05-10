using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Tests.TestDoubles;

public sealed record FakeFileEntry(string FullPath, long SizeBytes);

public sealed class FakeFileSystem : IFileSystem
{
    private readonly IReadOnlyList<FakeFileEntry> _files;

    public FakeFileSystem(IReadOnlyList<FakeFileEntry> files)
    {
        _files = files;
    }

    public async IAsyncEnumerable<FileSystemVideoCandidate> EnumerateFilesAsync(
        IReadOnlyCollection<string> roots,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var file in _files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return new FileSystemVideoCandidate(file.FullPath, file.SizeBytes);
        }
    }
}
