using FluentAssertions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.Tests.Persistence;

public sealed class SqliteRepositoryTests
{
    [Fact]
    public async Task Save_and_search_media_by_tag_and_path()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var mediaRepository = new SqliteMediaRepository(factory);
        var tagRepository = new SqliteTagRepository(factory);

        var tag = new TagDefinition(TagKey.Parse("country.soviet_union"), "苏联", "Soviet Union", ["USSR", "СССР"], TagKey.Parse("country"));
        await tagRepository.UpsertAsync(tag, CancellationToken.None);

        var item = new MediaItem(
            Guid.NewGuid(),
            MediaLibraryType.Normal,
            MediaStatus.Pending,
            @"D:\Movies\Soviet\film.mkv",
            "film.mkv",
            "D:",
            2_000_000_000,
            TimeSpan.FromMinutes(125),
            1920,
            1080,
            "未识别影片",
            null,
            1972,
            null,
            [tag.Key],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        await mediaRepository.UpsertAsync(item, CancellationToken.None);

        var results = await mediaRepository.SearchAsync("苏联", MediaLibraryType.Normal, includeAdultWhenUnlocked: false, CancellationToken.None);

        results.Should().ContainSingle();
        results[0].FilePath.Should().Be(item.FilePath);
    }
}
