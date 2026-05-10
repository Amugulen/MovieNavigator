using FluentAssertions;
using MovieNavigator.Core.Scanning;

namespace MovieNavigator.Tests.Scanning;

public sealed class ScanRulesTests
{
    [Fact]
    public void Default_rules_accept_long_video_in_authorized_directory()
    {
        var rules = ScanRules.CreateDefault([@"D:\Movies"]);

        var accepted = rules.ShouldConsiderPath(@"D:\Movies\Soviet\film.mkv");

        accepted.Should().BeTrue();
    }

    [Theory]
    [InlineData(@"D:\Games\cutscene.mp4")]
    [InlineData(@"D:\Movies\Cache\clip.mp4")]
    [InlineData(@"D:\Movies\Temp\clip.mp4")]
    public void Default_rules_reject_excluded_directories(string path)
    {
        var rules = ScanRules.CreateDefault([@"D:\Movies", @"D:\Games"]);

        rules.ShouldConsiderPath(path).Should().BeFalse();
    }

    [Theory]
    [InlineData("clip.txt")]
    [InlineData("image.jpg")]
    public void Default_rules_reject_non_video_extensions(string fileName)
    {
        var rules = ScanRules.CreateDefault([@"D:\Movies"]);

        rules.ShouldConsiderPath($@"D:\Movies\{fileName}").Should().BeFalse();
    }

    [Theory]
    [InlineData(19, 200_000_000, false)]
    [InlineData(40, 50_000_000, false)]
    [InlineData(40, 200_000_000, true)]
    public void Default_rules_apply_duration_and_size_thresholds(int minutes, long bytes, bool expected)
    {
        var rules = ScanRules.CreateDefault([@"D:\Movies"]);

        var result = rules.ShouldIncludeAnalyzedVideo(TimeSpan.FromMinutes(minutes), bytes);

        result.Should().Be(expected);
    }
}
