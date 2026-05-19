using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Anchor.Domain.Issues;
using Microsoft.Extensions.Options;

namespace Anchor.Web.Issues;

public sealed class GitHubIssueService(HttpClient httpClient, IOptions<GitHubIssueOptions> optionsAccessor) : IGitHubIssueService
{
    private const string GitHubApiVersion = "2022-11-28";
    private static readonly Uri DefaultApiBaseAddress = new("https://api.github.com/", UriKind.Absolute);
    private readonly GitHubIssueOptions options = optionsAccessor.Value;

    public async Task<GitHubIssueCreationResult> CreateIssueAsync(CreateGitHubIssueRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return GitHubIssueCreationResult.Failure("GitHub issue title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return GitHubIssueCreationResult.Failure("GitHub issue body is required.");
        }

        var configurationError = ValidateConfiguration(request);
        if (configurationError is not null)
        {
            return GitHubIssueCreationResult.Failure(configurationError);
        }

        ConfigureClientHeaders();

        CreatedGitHubIssue? createdIssue = null;

        try
        {
            createdIssue = await CreateRepositoryIssueAsync(title, request.Body, NormalizeLabels(request.Labels), cancellationToken);

            if (!request.AddToConfiguredProject)
            {
                return GitHubIssueCreationResult.Success(createdIssue.Number, createdIssue.Url);
            }

            var projectConfiguration = await GetProjectConfigurationAsync(request.ProjectStatusName, cancellationToken);
            var projectItemId = await AddIssueToProjectAsync(projectConfiguration.ProjectId, createdIssue.NodeId, cancellationToken);
            await UpdateProjectItemStatusAsync(projectConfiguration, projectItemId, cancellationToken);

            return GitHubIssueCreationResult.Success(createdIssue.Number, createdIssue.Url);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GitHubIssueCreationResult.Failure(ex.Message, createdIssue?.Number, createdIssue?.Url);
        }
    }

    private string? ValidateConfiguration(CreateGitHubIssueRequest request)
    {
        if (!options.Enabled)
        {
            return "GitHub issue creation is disabled.";
        }

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _))
        {
            return "GitHub issue API base URL is invalid.";
        }

        if (string.IsNullOrWhiteSpace(options.RepositoryOwner) || string.IsNullOrWhiteSpace(options.RepositoryName))
        {
            return "GitHub repository owner and repository name must be configured.";
        }

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return "GitHub issue access token is not configured.";
        }

        if (!request.AddToConfiguredProject)
        {
            return null;
        }

        if (options.ProjectNumber <= 0)
        {
            return "GitHub project number must be configured when project placement is enabled.";
        }

        if (string.IsNullOrWhiteSpace(options.ProjectStatusFieldName))
        {
            return "GitHub project status field name must be configured when project placement is enabled.";
        }

        var projectStatusName = ResolveProjectStatusName(request.ProjectStatusName);
        if (string.IsNullOrWhiteSpace(projectStatusName))
        {
            return "GitHub project status name must be configured when project placement is enabled.";
        }

        return null;
    }

    private void ConfigureClientHeaders()
    {
        if (!httpClient.DefaultRequestHeaders.Accept.Any(header => string.Equals(header.MediaType, "application/vnd.github+json", StringComparison.OrdinalIgnoreCase)))
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AnchorBarAndGrillWebsite");
        }

        if (!httpClient.DefaultRequestHeaders.Contains("X-GitHub-Api-Version"))
        {
            httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", GitHubApiVersion);
        }
    }

    private async Task<CreatedGitHubIssue> CreateRepositoryIssueAsync(string title, string body, string[] labels, CancellationToken cancellationToken)
    {
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            $"repos/{Uri.EscapeDataString(options.RepositoryOwner)}/{Uri.EscapeDataString(options.RepositoryName)}/issues",
            new
            {
                title,
                body,
                labels
            });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseJson = await ReadResponseJsonAsync(response, cancellationToken);

        EnsureRestSuccess(response, responseJson, "create the GitHub issue");

        var issueNumber = responseJson?["number"]?.GetValue<int?>()
            ?? throw new InvalidOperationException("GitHub issue creation did not return an issue number.");
        var issueUrl = responseJson?["html_url"]?.GetValue<string>()
            ?? throw new InvalidOperationException("GitHub issue creation did not return an issue URL.");
        var issueNodeId = responseJson?["node_id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("GitHub issue creation did not return an issue node ID.");

        return new CreatedGitHubIssue(issueNumber, issueUrl, issueNodeId);
    }

    private async Task<ProjectConfiguration> GetProjectConfigurationAsync(string? requestedStatusName, CancellationToken cancellationToken)
    {
        var responseJson = await SendGraphQlAsync(
            """
            query($owner: String!, $number: Int!) {
              user(login: $owner) {
                projectV2(number: $number) {
                  id
                  fields(first: 50) {
                    nodes {
                      ... on ProjectV2SingleSelectField {
                        id
                        name
                        options {
                          id
                          name
                        }
                      }
                    }
                  }
                }
              }
              organization(login: $owner) {
                projectV2(number: $number) {
                  id
                  fields(first: 50) {
                    nodes {
                      ... on ProjectV2SingleSelectField {
                        id
                        name
                        options {
                          id
                          name
                        }
                      }
                    }
                  }
                }
              }
            }
            """,
            new
            {
                owner = ResolveProjectOwner(),
                number = options.ProjectNumber
            },
            cancellationToken);

        var projectNode = responseJson?["data"]?["user"]?["projectV2"] ?? responseJson?["data"]?["organization"]?["projectV2"];
        if (projectNode is null)
        {
            throw new InvalidOperationException("Configured GitHub project was not found.");
        }

        var projectId = projectNode["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Configured GitHub project did not return a project ID.");

        var statusField = projectNode["fields"]?["nodes"]?
            .AsArray()
            .OfType<JsonObject>()
            .FirstOrDefault(field =>
                string.Equals(field["name"]?.GetValue<string>(), options.ProjectStatusFieldName, StringComparison.Ordinal));

        if (statusField is null)
        {
            throw new InvalidOperationException($"Configured GitHub project status field '{options.ProjectStatusFieldName}' was not found.");
        }

        var requestedStatus = ResolveProjectStatusName(requestedStatusName);
        var statusOption = statusField["options"]?
            .AsArray()
            .OfType<JsonObject>()
            .FirstOrDefault(option =>
                string.Equals(option["name"]?.GetValue<string>(), requestedStatus, StringComparison.Ordinal));

        if (statusOption is null)
        {
            throw new InvalidOperationException($"Configured GitHub project status '{requestedStatus}' was not found.");
        }

        var statusFieldId = statusField["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Configured GitHub project status field did not return an ID.");
        var statusOptionId = statusOption["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Configured GitHub project status option did not return an ID.");

        return new ProjectConfiguration(projectId, statusFieldId, statusOptionId);
    }

    private async Task<string> AddIssueToProjectAsync(string projectId, string issueNodeId, CancellationToken cancellationToken)
    {
        var responseJson = await SendGraphQlAsync(
            """
            mutation($projectId: ID!, $contentId: ID!) {
              addProjectV2ItemById(input: {
                projectId: $projectId,
                contentId: $contentId
              }) {
                item {
                  id
                }
              }
            }
            """,
            new
            {
                projectId,
                contentId = issueNodeId
            },
            cancellationToken);

        return responseJson?["data"]?["addProjectV2ItemById"]?["item"]?["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("GitHub project item creation did not return an item ID.");
    }

    private async Task UpdateProjectItemStatusAsync(ProjectConfiguration configuration, string projectItemId, CancellationToken cancellationToken)
    {
        await SendGraphQlAsync(
            """
            mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
              updateProjectV2ItemFieldValue(input: {
                projectId: $projectId,
                itemId: $itemId,
                fieldId: $fieldId,
                value: {
                  singleSelectOptionId: $optionId
                }
              }) {
                projectV2Item {
                  id
                }
              }
            }
            """,
            new
            {
                projectId = configuration.ProjectId,
                itemId = projectItemId,
                fieldId = configuration.StatusFieldId,
                optionId = configuration.StatusOptionId
            },
            cancellationToken);
    }

    private async Task<JsonNode?> SendGraphQlAsync(string query, object variables, CancellationToken cancellationToken)
    {
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "graphql",
            new
            {
                query,
                variables
            });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseJson = await ReadResponseJsonAsync(response, cancellationToken);

        EnsureRestSuccess(response, responseJson, "call the GitHub GraphQL API");

        var graphQlErrors = responseJson?["errors"]?.AsArray();
        if (graphQlErrors is { Count: > 0 })
        {
            var messages = graphQlErrors
                .OfType<JsonObject>()
                .Select(error => error["message"]?.GetValue<string>())
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(message => message!.Trim())
                .ToArray();

            throw new InvalidOperationException(
                messages.Length == 0
                    ? "GitHub GraphQL API returned an error."
                    : string.Join(" ", messages));
        }

        return responseJson;
    }

    private HttpRequestMessage CreateJsonRequest(HttpMethod method, string relativePath, object payload)
    {
        var request = new HttpRequestMessage(method, new Uri(GetApiBaseAddress(), relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        request.Content = JsonContent.Create(payload);
        return request;
    }

    private Uri GetApiBaseAddress() =>
        Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out var configuredUri)
            ? configuredUri
            : DefaultApiBaseAddress;

    private string ResolveProjectOwner() =>
        string.IsNullOrWhiteSpace(options.ProjectOwner)
            ? options.RepositoryOwner?.Trim() ?? string.Empty
            : options.ProjectOwner.Trim();

    private string ResolveProjectStatusName(string? requestedStatusName) =>
        string.IsNullOrWhiteSpace(requestedStatusName)
            ? options.DefaultProjectStatusName?.Trim() ?? string.Empty
            : requestedStatusName.Trim();

    private static string[] NormalizeLabels(IReadOnlyCollection<string> labels) =>
        labels
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(label => label.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static async Task<JsonNode?> ReadResponseJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content is null)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(content)
            ? null
            : JsonNode.Parse(content);
    }

    private static void EnsureRestSuccess(HttpResponseMessage response, JsonNode? responseJson, string operationDescription)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = BuildErrorDetail(responseJson);
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(detail)
                ? $"GitHub API failed to {operationDescription}."
                : $"GitHub API failed to {operationDescription}: {detail}");
    }

    private static string BuildErrorDetail(JsonNode? responseJson)
    {
        var messages = new List<string>();

        var topLevelMessage = responseJson?["message"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(topLevelMessage))
        {
            messages.Add(topLevelMessage.Trim());
        }

        if (responseJson?["errors"] is JsonArray errors)
        {
            foreach (var error in errors)
            {
                switch (error)
                {
                    case JsonValue value when value.TryGetValue<string>(out var textValue) && !string.IsNullOrWhiteSpace(textValue):
                        messages.Add(textValue.Trim());
                        break;
                    case JsonObject errorObject:
                    {
                        var message = errorObject["message"]?.GetValue<string>()
                            ?? errorObject["code"]?.GetValue<string>()
                            ?? errorObject["resource"]?.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            messages.Add(message.Trim());
                        }

                        break;
                    }
                }
            }
        }

        return string.Join(" ", messages.Distinct(StringComparer.Ordinal));
    }

    private sealed record CreatedGitHubIssue(int Number, string Url, string NodeId);

    private sealed record ProjectConfiguration(string ProjectId, string StatusFieldId, string StatusOptionId);
}
