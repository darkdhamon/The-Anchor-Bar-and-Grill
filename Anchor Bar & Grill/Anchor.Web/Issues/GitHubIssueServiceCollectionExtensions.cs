using Anchor.Domain.Issues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Issues;

public static class GitHubIssueServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubIssueServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GitHubIssueOptions>(configuration.GetSection(GitHubIssueOptions.SectionName));
        services.AddHttpClient<IGitHubIssueService, GitHubIssueService>();

        return services;
    }
}
