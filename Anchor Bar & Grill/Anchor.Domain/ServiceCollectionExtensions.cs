using Anchor.Domain.Events;
using Anchor.Domain.Identity.Bootstrap;
using Anchor.Domain.Identity.Configuration;
using Anchor.Domain.Identity.Users;
using Anchor.Domain.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Domain;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IEventQueryService, EventQueryService>();
        services.AddScoped<IEventManagementService, EventManagementService>();
        services.AddScoped<IConfirmedAccountAccessService, ConfirmedAccountAccessService>();
        services.AddScoped<IConfirmedAccountConfigurationService, ConfirmedAccountConfigurationService>();
        services.AddScoped<IIdentityAdministrationService, IdentityAdministrationService>();
        services.AddScoped<IIdentityBootstrapService, IdentityBootstrapService>();
        services.AddScoped<IMenuQueryService, MenuQueryService>();
        services.AddScoped<IMenuManagementService, MenuManagementService>();

        return services;
    }
}
