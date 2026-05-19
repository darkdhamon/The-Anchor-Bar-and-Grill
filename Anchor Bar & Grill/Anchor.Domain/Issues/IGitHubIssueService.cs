namespace Anchor.Domain.Issues;

public interface IGitHubIssueService
{
    Task<GitHubIssueCreationResult> CreateIssueAsync(CreateGitHubIssueRequest request, CancellationToken cancellationToken = default);
}
