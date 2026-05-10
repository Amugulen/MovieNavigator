namespace MovieNavigator.Core.Abstractions;

public interface IFileSystem
{
    IAsyncEnumerable<FileSystemVideoCandidate> EnumerateFilesAsync(IReadOnlyCollection<string> roots, CancellationToken cancellationToken);
}

public sealed record FileSystemVideoCandidate(string FullPath, long SizeBytes);
