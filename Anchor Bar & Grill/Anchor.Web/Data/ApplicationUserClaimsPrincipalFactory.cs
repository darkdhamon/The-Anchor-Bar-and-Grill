using System.Security.Claims;
using Anchor.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Anchor.Web.Data;

public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Surname, user.LastName));
        }

        if (user.MustChangePassword)
        {
            identity.AddClaim(new Claim(ApplicationClaimTypes.MustChangePassword, bool.TrueString.ToLowerInvariant()));
        }

        return identity;
    }
}
