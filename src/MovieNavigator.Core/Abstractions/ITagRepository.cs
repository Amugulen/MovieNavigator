using MovieNavigator.Core.Tags;

namespace MovieNavigator.Core.Abstractions;

public interface ITagRepository
{
    Task UpsertAsync(TagDefinition tag, CancellationToken cancellationToken);
    Task<IReadOnlyList<TagDefinition>> GetTreeAsync(CancellationToken cancellationToken);
}
