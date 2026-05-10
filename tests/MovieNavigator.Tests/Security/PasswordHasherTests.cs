using FluentAssertions;
using MovieNavigator.Core.Security;

namespace MovieNavigator.Tests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_accepts_correct_password_and_rejects_wrong_password()
    {
        var hash = PasswordHasher.Hash("123456");

        PasswordHasher.Verify("123456", hash).Should().BeTrue();
        PasswordHasher.Verify("000000", hash).Should().BeFalse();
        hash.Should().NotContain("123456");
    }
}
