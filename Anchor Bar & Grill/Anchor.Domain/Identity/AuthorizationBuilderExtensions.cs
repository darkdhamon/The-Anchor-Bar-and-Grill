using Microsoft.AspNetCore.Authorization;

namespace Anchor.Domain.Identity;

public static class AuthorizationBuilderExtensions
{
    public static AuthorizationBuilder AddAnchorAuthorizationPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .AddPolicy(ApplicationPolicies.AdminAccess, policy => policy.RequireRole(ApplicationRoles.Admin))
            .AddPolicy(ApplicationPolicies.EventManagement, policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.EventManager))
            .AddPolicy(ApplicationPolicies.MenuManagement, policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.MenuManager))
            .AddPolicy(ApplicationPolicies.ITAccess, policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.It));

        return builder;
    }
}
