namespace Anchor.Domain.Issues;

public sealed class GitHubIssueCreationResult
{
    private GitHubIssueCreationResult()
    {
    }

    public bool Succeeded { get; private init; }

    public int? IssueNumber { get; private init; }

    public string? IssueUrl { get; private init; }

    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    public static GitHubIssueCreationResult Success(int issueNumber, string issueUrl) =>
        new()
        {
            Succeeded = true,
            IssueNumber = issueNumber,
            IssueUrl = issueUrl
        };

    public static GitHubIssueCreationResult Failure(string error, int? issueNumber = null, string? issueUrl = null) =>
        Failure([error], issueNumber, issueUrl);

    public static GitHubIssueCreationResult Failure(IEnumerable<string> errors, int? issueNumber = null, string? issueUrl = null)
    {
        var normalizedErrors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Select(error => error.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new GitHubIssueCreationResult
        {
            Succeeded = false,
            IssueNumber = issueNumber,
            IssueUrl = issueUrl,
            Errors = normalizedErrors.Length == 0
                ? ["GitHub issue creation failed."]
                : normalizedErrors
        };
    }
}
