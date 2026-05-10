using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.Abstractions;

public interface IMediaRepository
{
    Task UpsertAsync(MediaItem item, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaItem>> SearchAsync(string query, MediaLibraryType libraryType, bool includeAdultWhenUnlocked, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaItem>> GetByDriveAsync(string driveKey, MediaLibraryType libraryType, CancellationToken cancellationToken);
}
