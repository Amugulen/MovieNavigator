using FluentAssertions;
using MovieNavigator.Core.Classification;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Tests.Classification;

public sealed class ClassificationFacetBuilderTests
{
    [Fact]
    public void Build_returns_facets_from_indexed_media()
    {
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
            "Soviet Film",
            null,
            1972,
            null,
            [TagKey.Parse("country.soviet_union"), TagKey.Parse("genre.war")],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var facets = ClassificationFacetBuilder.Build([item]);

        facets.Select(facet => facet.Key).Should().Contain([
            "storage.drive.d",
            "status.pending",
            "type.mkv",
            "decade.1970s",
            "resolution.1080p",
            "duration.long",
            "country.soviet_union",
            "genre.war"
        ]);

        facets.Single(facet => facet.Key == "storage.drive.d").Count.Should().Be(1);
    }
}
