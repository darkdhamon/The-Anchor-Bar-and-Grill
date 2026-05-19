using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Issues;

public static class ProductionExceptionIssueReporterServiceCollectionExtensions
{
    public static IServiceCollection AddProductionExceptionIssueReporting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<ProductionExceptionIssueOptions>(configuration.GetSection(ProductionExceptionIssueOptions.SectionName));
        services.AddScoped<IProductionExceptionIssueReporter, ProductionExceptionIssueReporter>();

        return services;
    }
}
