namespace Anchor.Web.Issues;

public sealed class GitHubIssueOptions
{
    public const string SectionName = "GitHubIssues";

    public string ApiBaseUrl { get; set; } = "https://api.github.com/";

    public bool Enabled { get; set; }

    public string RepositoryOwner { get; set; } = string.Empty;

    public string RepositoryName { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string ProjectOwner { get; set; } = string.Empty;

    public int ProjectNumber { get; set; }

    public string ProjectStatusFieldName { get; set; } = "Status";

    public string DefaultProjectStatusName { get; set; } = "Backlog";
}
