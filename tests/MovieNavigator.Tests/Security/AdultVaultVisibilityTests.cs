using FluentAssertions;
using MovieNavigator.Core.Security;

namespace MovieNavigator.Tests.Security;

public sealed class AdultVaultVisibilityTests
{
    [Fact]
    public void Locked_vault_blocks_adult_queries()
    {
        var state = AdultVaultState.Locked();

        state.CanQueryAdultLibrary.Should().BeFalse();
        state.CanShowAdultTags.Should().BeFalse();
    }

    [Fact]
    public void Unlocked_vault_allows_adult_queries()
    {
        var state = AdultVaultState.Unlocked(DateTimeOffset.UtcNow.AddMinutes(15));

        state.CanQueryAdultLibrary.Should().BeTrue();
        state.CanShowAdultTags.Should().BeTrue();
    }
}
