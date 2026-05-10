using FluentAssertions;
using MovieNavigator.App.Localization;

namespace MovieNavigator.Tests.Localization;

public sealed class JsonAppLocalizerTests
{
    [Fact]
    public void Get_returns_current_culture_text()
    {
        var localizer = JsonAppLocalizer.FromDictionaries(
            "zh-CN",
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["zh-CN"] = new Dictionary<string, string> { ["Nav.Home"] = "首页" },
                ["en-US"] = new Dictionary<string, string> { ["Nav.Home"] = "Home" }
            });

        localizer.Get("Nav.Home").Should().Be("首页");
    }

    [Fact]
    public void Get_falls_back_to_english_then_key()
    {
        var localizer = JsonAppLocalizer.FromDictionaries(
            "zh-CN",
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["zh-CN"] = new Dictionary<string, string>(),
                ["en-US"] = new Dictionary<string, string> { ["Action.OpenDefaultPlayer"] = "Open with default player" }
            });

        localizer.Get("Action.OpenDefaultPlayer").Should().Be("Open with default player");
        localizer.Get("Missing.Key").Should().Be("Missing.Key");
    }
}
