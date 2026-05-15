using System.Security.Claims;

namespace Anchor.Domain.Tests.Identity;

public sealed class ForcedPasswordChangeEvaluatorTests
{
    [Fact]
    public void ShouldRedirect_returns_false_for_unauthenticated_users()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = ForcedPasswordChangeEvaluator.ShouldRedirect(user, typeof(StandardPage), typeof(ChangePasswordPage));

        Assert.False(result);
    }

    [Fact]
    public void ShouldRedirect_returns_false_for_the_change_password_page()
    {
        var user = CreateUser(mustChangePassword: true);

        var result = ForcedPasswordChangeEvaluator.ShouldRedirect(user, typeof(ChangePasswordPage), typeof(ChangePasswordPage));

        Assert.False(result);
    }

    [Fact]
    public void ShouldRedirect_returns_true_for_authenticated_users_who_must_change_password()
    {
        var user = CreateUser(mustChangePassword: true);

        var result = ForcedPasswordChangeEvaluator.ShouldRedirect(user, typeof(StandardPage), typeof(ChangePasswordPage));

        Assert.True(result);
    }

    [Fact]
    public void ShouldRedirect_returns_false_when_claim_is_missing_or_false()
    {
        var user = CreateUser(mustChangePassword: false);

        var result = ForcedPasswordChangeEvaluator.ShouldRedirect(user, typeof(StandardPage), typeof(ChangePasswordPage));

        Assert.False(result);
    }

    private static ClaimsPrincipal CreateUser(bool mustChangePassword) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ApplicationClaimTypes.MustChangePassword, mustChangePassword.ToString())
        ], "TestAuth"));

    private sealed class StandardPage;

    private sealed class ChangePasswordPage;
}
