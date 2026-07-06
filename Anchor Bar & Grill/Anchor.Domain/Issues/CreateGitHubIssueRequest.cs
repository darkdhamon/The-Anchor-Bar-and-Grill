namespace Anchor.Domain.Issues;

public sealed class CreateGitHubIssueRequest
{
    public required string Title { get; init; }

    public required string Body { get; init; }

    public IReadOnlyCollection<string> Labels { get; init; } = Array.Empty<string>();

    public bool AddToConfiguredProject { get; init; } = true;

    public string? ProjectStatusName { get; init; }
}
