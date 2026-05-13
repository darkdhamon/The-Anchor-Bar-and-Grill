using System.Security.Claims;

namespace Anchor.Domain.Identity;

public static class ForcedPasswordChangeEvaluator
{
    public static bool ShouldRedirect(ClaimsPrincipal user, Type currentPageType, Type changePasswordPageType)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(currentPageType);
        ArgumentNullException.ThrowIfNull(changePasswordPageType);

        if (user.Identity?.IsAuthenticated != true || currentPageType == changePasswordPageType)
        {
            return false;
        }

        return user.Claims.Any(claim =>
            claim.Type == ApplicationClaimTypes.MustChangePassword &&
            string.Equals(claim.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase));
    }
}
