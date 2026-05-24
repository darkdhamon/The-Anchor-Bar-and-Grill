using Anchor.Domain.Identity.Bootstrap;
using Anchor.Domain.Identity.Users;
using Anchor.Domain.Menu;
using Anchor.Infrastructure.Data.Menu;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<Data.ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IIdentityAdministrationRepository, Data.IdentityAdministrationRepository>();
        services.AddScoped<IIdentityBootstrapRepository, Data.IdentityBootstrapRepository>();
        services.AddScoped<IMenuQueryRepository, MenuQueryRepository>();
        services.AddScoped<IMenuManagementRepository, MenuManagementRepository>();
        services.AddSingleton<IMenuOperationLogSink, NoOpMenuOperationLogSink>();

        return services;
    }
}
