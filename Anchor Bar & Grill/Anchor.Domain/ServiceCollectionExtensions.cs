using Anchor.Domain.Identity.Bootstrap;
using Anchor.Domain.Identity.Configuration;
using Anchor.Domain.Identity.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Domain;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IConfirmedAccountConfigurationService, ConfirmedAccountConfigurationService>();
        services.AddScoped<IIdentityAdministrationService, IdentityAdministrationService>();
        services.AddScoped<IIdentityBootstrapService, IdentityBootstrapService>();

        return services;
    }
}
