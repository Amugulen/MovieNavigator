using FluentAssertions;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Tests.Tags;

public sealed class TagKeyTests
{
    [Theory]
    [InlineData("country.soviet_union")]
    [InlineData("person.director.akira_kurosawa")]
    [InlineData("storage.drive.d")]
    public void Parse_accepts_lowercase_dotted_tag_keys(string raw)
    {
        var key = TagKey.Parse(raw);

        key.Value.Should().Be(raw);
        key.ParentKey.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Country.Soviet")]
    [InlineData("country..soviet")]
    [InlineData(".country")]
    [InlineData("country.")]
    [InlineData("country/soviet")]
    public void Parse_rejects_invalid_tag_keys(string raw)
    {
        var act = () => TagKey.Parse(raw);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Root_key_has_no_parent()
    {
        var key = TagKey.Parse("country");

        key.ParentKey.Should().BeNull();
    }
}
