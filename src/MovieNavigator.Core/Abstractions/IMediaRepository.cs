using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Core.Abstractions;

public interface IMediaRepository
{
    Task UpsertAsync(MediaItem item, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaItem>> GetAllAsync(MediaLibraryType libraryType, bool includeAdultWhenUnlocked, CancellationToken cancellationToken);
    Task<MediaItem?> GetByPathAsync(string filePath, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaItem>> SearchAsync(string query, MediaLibraryType libraryType, bool includeAdultWhenUnlocked, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaItem>> GetByDriveAsync(string driveKey, MediaLibraryType libraryType, CancellationToken cancellationToken);
    Task MarkMissingAsync(string filePath, DateTimeOffset missingSince, CancellationToken cancellationToken);
    Task AddTagsAsync(string filePath, IReadOnlyCollection<TagKey> tags, CancellationToken cancellationToken);
}
