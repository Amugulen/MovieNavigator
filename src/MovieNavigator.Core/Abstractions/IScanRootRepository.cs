using MovieNavigator.Core.Indexing;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.Abstractions;

public interface IScanRootRepository
{
    Task UpsertAsync(ScanRoot root, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScanRoot>> GetEnabledAsync(MediaLibraryType libraryType, CancellationToken cancellationToken);
    Task UpdateLastScanAtAsync(string path, DateTimeOffset scannedAt, CancellationToken cancellationToken);
}
