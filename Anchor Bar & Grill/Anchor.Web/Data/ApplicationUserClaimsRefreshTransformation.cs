using System.Security.Claims;
using Anchor.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Anchor.Web.Data;

public sealed class ApplicationUserClaimsRefreshTransformation(
    UserManager<ApplicationUser> userManager,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return principal;
        }

        return await claimsPrincipalFactory.CreateAsync(user);
    }
}
