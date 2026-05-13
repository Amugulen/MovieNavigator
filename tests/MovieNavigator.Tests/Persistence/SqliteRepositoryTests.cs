using FluentAssertions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.Tests.Persistence;

public sealed class SqliteRepositoryTests
{
    [Fact]
    public async Task Get_all_returns_saved_media_after_repository_recreated()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);

        var firstRepository = new SqliteMediaRepository(factory);
        var sovietTag = TagKey.Parse("country.soviet_union");
        var first = CreateMediaItem(
            @"D:\Movies\Soviet\film.mkv",
            "film.mkv",
            "Soviet Film",
            [sovietTag]);
        var second = CreateMediaItem(
            @"E:\Movies\Animation\family.mp4",
            "family.mp4",
            "Family Animation",
            []);

        await firstRepository.UpsertAsync(first, CancellationToken.None);
        await firstRepository.UpsertAsync(second, CancellationToken.None);

        var recreatedRepository = new SqliteMediaRepository(factory);
        var results = await recreatedRepository.GetAllAsync(MediaLibraryType.Normal, includeAdultWhenUnlocked: false, CancellationToken.None);

        results.Should().HaveCount(2);
        results.Select(item => item.FilePath).Should().BeEquivalentTo(first.FilePath, second.FilePath);
        results.Single(item => item.FilePath == first.FilePath).Tags.Should().ContainSingle(tag => tag == sovietTag);
    }

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

    [Fact]
    public async Task Save_and_load_media_thumbnail_and_file_metadata()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var repository = new SqliteMediaRepository(factory);
        var lastWrite = DateTimeOffset.Parse("2026-05-10T08:00:00+00:00");
        var missingSince = DateTimeOffset.Parse("2026-05-10T09:00:00+00:00");
        var item = CreateMediaItem(@"D:\Movies\film.mkv", "film.mkv", "Film", [])
            with
            {
                ThumbnailPath = @"C:\Users\asus\AppData\Local\MovieNavigator\thumbnails\film.jpg",
                Extension = ".mkv",
                LastWriteTimeUtc = lastWrite,
                MissingSince = missingSince
            };

        await repository.UpsertAsync(item, CancellationToken.None);

        var loaded = await repository.GetByPathAsync(item.FilePath, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.ThumbnailPath.Should().Be(item.ThumbnailPath);
        loaded.Extension.Should().Be(".mkv");
        loaded.LastWriteTimeUtc.Should().Be(lastWrite);
        loaded.MissingSince.Should().Be(missingSince);
    }

    [Fact]
    public async Task Add_tags_merges_new_tags_and_refreshes_search_index()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var mediaRepository = new SqliteMediaRepository(factory);
        var tagRepository = new SqliteTagRepository(factory);
        var originalTag = TagKey.Parse("country.soviet_union");
        var aiTag = TagKey.Parse("genre.war");
        await tagRepository.UpsertAsync(new TagDefinition(originalTag, "苏联", "Soviet Union", [], TagKey.Parse("country")), CancellationToken.None);
        await tagRepository.UpsertAsync(new TagDefinition(aiTag, "战争", "War", [], TagKey.Parse("genre")), CancellationToken.None);
        var item = CreateMediaItem(@"D:\Movies\film.mkv", "film.mkv", "Film", [originalTag]);
        await mediaRepository.UpsertAsync(item, CancellationToken.None);

        await mediaRepository.AddTagsAsync(item.FilePath, [originalTag, aiTag], CancellationToken.None);

        var loaded = await mediaRepository.GetByPathAsync(item.FilePath, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Tags.Select(tag => tag.Value).Should().BeEquivalentTo("country.soviet_union", "genre.war");

        var searchResults = await mediaRepository.SearchAsync("战争", MediaLibraryType.Normal, includeAdultWhenUnlocked: false, CancellationToken.None);
        searchResults.Should().ContainSingle(result => result.FilePath == item.FilePath);
    }


    private static MediaItem CreateMediaItem(string filePath, string fileName, string title, IReadOnlyCollection<TagKey> tags)
    {
        var now = DateTimeOffset.UtcNow;
        return new MediaItem(
            Guid.NewGuid(),
            MediaLibraryType.Normal,
            MediaStatus.Pending,
            filePath,
            fileName,
            Path.GetPathRoot(filePath)?.TrimEnd('\\') ?? "unknown",
            2_000_000_000,
            TimeSpan.FromMinutes(100),
            1920,
            1080,
            title,
            null,
            null,
            null,
            tags,
            now,
            now);
    }
}
