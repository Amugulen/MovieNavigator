using FluentAssertions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.Tests.Search;

public sealed class SearchTests
{
    [Fact]
    public async Task Normal_search_does_not_return_adult_items_when_vault_locked()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var repository = new SqliteMediaRepository(factory);

        await repository.UpsertAsync(CreateItem(MediaLibraryType.Normal, @"D:\Movies\normal.mkv", "普通影片", "D:", [TagKey.Parse("genre.war")]), CancellationToken.None);
        await repository.UpsertAsync(CreateItem(MediaLibraryType.Adult, @"X:\Adult\adult.mkv", "成人影片", "X:", [TagKey.Parse("adult.topic.sample")]), CancellationToken.None);

        var results = await repository.SearchAsync("影片", MediaLibraryType.Normal, includeAdultWhenUnlocked: false, CancellationToken.None);

        results.Should().ContainSingle();
        results[0].LibraryType.Should().Be(MediaLibraryType.Normal);
    }

    private static MediaItem CreateItem(MediaLibraryType libraryType, string path, string title, string drive, IReadOnlyCollection<TagKey> tags)
    {
        return new MediaItem(Guid.NewGuid(), libraryType, MediaStatus.Confirmed, path, Path.GetFileName(path), drive, 200_000_000, TimeSpan.FromMinutes(90), 1920, 1080, title, null, 1970, null, tags, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }
}
