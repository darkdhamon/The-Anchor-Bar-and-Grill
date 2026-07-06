using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Anchor.Domain.Issues;
using Anchor.Web.Issues;
using Microsoft.Extensions.Options;

namespace Anchor.Web.Tests.Issues;

public sealed class GitHubIssueServiceTests
{
    [Fact]
    public async Task CreateIssueAsync_creates_issue_and_places_it_in_configured_project_backlog()
    {
        var handler = new SequenceHttpMessageHandler(
            async (request, cancellationToken) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("https://api.github.com/repos/darkdhamon/The-Anchor-Bar-and-Grill/issues", request.RequestUri?.ToString());
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("ghs_test_token", request.Headers.Authorization?.Parameter);

                var payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
                Assert.Equal("Production exception", payload["title"]?.GetValue<string>());
                Assert.Equal("Stack trace and request details.", payload["body"]?.GetValue<string>());
                Assert.Equal(["bug", "production"], payload["labels"]!.AsArray().Select(label => label!.GetValue<string>()).ToArray());

                return CreateJsonResponse(
                    HttpStatusCode.Created,
                    """
                    {
                      "number": 42,
                      "html_url": "https://github.com/darkdhamon/The-Anchor-Bar-and-Grill/issues/42",
                      "node_id": "I_kwDOAnchor42"
                    }
                    """);
            },
            async (request, cancellationToken) =>
            {
                Assert.Equal("https://api.github.com/graphql", request.RequestUri?.ToString());

                var payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
                Assert.Contains("projectV2", payload["query"]?.GetValue<string>(), StringComparison.Ordinal);
                Assert.Contains("user(login: $owner)", payload["query"]?.GetValue<string>(), StringComparison.Ordinal);
                Assert.DoesNotContain("organization(login: $owner)", payload["query"]?.GetValue<string>(), StringComparison.Ordinal);
                Assert.Equal("darkdhamon", payload["variables"]?["owner"]?.GetValue<string>());
                Assert.Equal(7, payload["variables"]?["number"]?.GetValue<int>());

                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "data": {
                        "user": {
                          "projectV2": {
                            "id": "PVT_anchor",
                            "fields": {
                              "nodes": [
                                {
                                  "id": "PVTSSF_status",
                                  "name": "Status",
                                  "options": [
                                    { "id": "option_backlog", "name": "Backlog" },
                                    { "id": "option_ready", "name": "Ready" }
                                  ]
                                }
                              ]
                        }
                      }
                    }
                  }
                }
                """);
            },
            async (request, cancellationToken) =>
            {
                var payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
                Assert.Contains("addProjectV2ItemById", payload["query"]?.GetValue<string>(), StringComparison.Ordinal);
                Assert.Equal("PVT_anchor", payload["variables"]?["projectId"]?.GetValue<string>());
                Assert.Equal("I_kwDOAnchor42", payload["variables"]?["contentId"]?.GetValue<string>());

                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "data": {
                        "addProjectV2ItemById": {
                          "item": {
                            "id": "PVTI_anchor_item"
                          }
                        }
                      }
                    }
                    """);
            },
            async (request, cancellationToken) =>
            {
                var payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
                Assert.Contains("updateProjectV2ItemFieldValue", payload["query"]?.GetValue<string>(), StringComparison.Ordinal);
                Assert.Equal("PVT_anchor", payload["variables"]?["projectId"]?.GetValue<string>());
                Assert.Equal("PVTI_anchor_item", payload["variables"]?["itemId"]?.GetValue<string>());
                Assert.Equal("PVTSSF_status", payload["variables"]?["fieldId"]?.GetValue<string>());
                Assert.Equal("option_backlog", payload["variables"]?["optionId"]?.GetValue<string>());

                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "data": {
                        "updateProjectV2ItemFieldValue": {
                          "projectV2Item": {
                            "id": "PVTI_anchor_item"
                          }
                        }
                      }
                    }
                    """);
            });

        var service = CreateService(handler);

        var result = await service.CreateIssueAsync(new CreateGitHubIssueRequest
        {
            Title = "Production exception",
            Body = "Stack trace and request details.",
            Labels = ["bug", "production", "bug"]
        });

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.IssueNumber);
        Assert.Equal("https://github.com/darkdhamon/The-Anchor-Bar-and-Grill/issues/42", result.IssueUrl);
        Assert.Empty(result.Errors);
        Assert.Equal(4, handler.RequestCount);
    }

    [Fact]
    public async Task CreateIssueAsync_skips_project_placement_when_request_disables_it()
    {
        var handler = new SequenceHttpMessageHandler(async (request, cancellationToken) =>
        {
            var payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            Assert.Equal("Customer-reported issue", payload["title"]?.GetValue<string>());

            return CreateJsonResponse(
                HttpStatusCode.Created,
                """
                {
                  "number": 84,
                  "html_url": "https://github.com/darkdhamon/The-Anchor-Bar-and-Grill/issues/84",
                  "node_id": "I_kwDOAnchor84"
                }
                """);
        });

        var service = CreateService(handler);

        var result = await service.CreateIssueAsync(new CreateGitHubIssueRequest
        {
            Title = "Customer-reported issue",
            Body = "Public site feedback details.",
            AddToConfiguredProject = false
        });

        Assert.True(result.Succeeded);
        Assert.Equal(84, result.IssueNumber);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task CreateIssueAsync_returns_failure_when_service_is_disabled()
    {
        var handler = new SequenceHttpMessageHandler();
        var service = CreateService(
            handler,
            new GitHubIssueOptions
            {
                Enabled = false,
                RepositoryOwner = "darkdhamon",
                RepositoryName = "The-Anchor-Bar-and-Grill",
                AccessToken = "ghs_test_token",
                ProjectNumber = 7
            });

        var result = await service.CreateIssueAsync(new CreateGitHubIssueRequest
        {
            Title = "Production exception",
            Body = "Stack trace and request details."
        });

        Assert.False(result.Succeeded);
        Assert.Equal(["GitHub issue creation is disabled."], result.Errors);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CreateIssueAsync_returns_failure_when_requested_project_status_is_missing()
    {
        var handler = new SequenceHttpMessageHandler(
            (_, _) => Task.FromResult(CreateJsonResponse(
                HttpStatusCode.Created,
                """
                {
                  "number": 41,
                  "html_url": "https://github.com/darkdhamon/The-Anchor-Bar-and-Grill/issues/41",
                  "node_id": "I_kwDOAnchor41"
                }
                """)),
            (_, _) => Task.FromResult(CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "data": {
                    "user": {
                      "projectV2": {
                        "id": "PVT_anchor",
                        "fields": {
                          "nodes": [
                            {
                              "id": "PVTSSF_status",
                              "name": "Status",
                              "options": [
                                { "id": "option_ready", "name": "Ready" }
                              ]
                            }
                          ]
                        }
                      }
                    },
                    "organization": null
                  }
                }
                """)));

        var service = CreateService(handler);

        var result = await service.CreateIssueAsync(new CreateGitHubIssueRequest
        {
            Title = "Production exception",
            Body = "Stack trace and request details.",
            ProjectStatusName = "Backlog"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(41, result.IssueNumber);
        Assert.Equal("https://github.com/darkdhamon/The-Anchor-Bar-and-Grill/issues/41", result.IssueUrl);
        Assert.Contains("Configured GitHub project status 'Backlog' was not found.", result.Errors.Single(), StringComparison.Ordinal);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task CreateIssueAsync_places_issue_in_configured_organization_project()
    {
        var handler = new SequenceHttpMessageHandler(
            (_, _) => Task.FromResult(CreateJsonResponse(
                HttpStatusCode.Created,
                """
                {
                  "number": 43,
                  "html_url": "https://github.com/anchor-bar/The-Anchor-Bar-and-Grill/issues/43",
                  "node_id": "I_kwDOAnchor43"
                }
                """)),
            async (request, cancellationToken) =>
            {
                var payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
                Assert.Contains("organization(login: $owner)", payload["query"]?.GetValue<string>(), StringComparison.Ordinal);
                Assert.DoesNotContain("user(login: $owner)", payload["query"]?.GetValue<string>(), StringComparison.Ordinal);
                Assert.Equal("anchor-bar", payload["variables"]?["owner"]?.GetValue<string>());

                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "data": {
                        "organization": {
                          "projectV2": {
                            "id": "PVT_anchor_org",
                            "fields": {
                              "nodes": [
                                {
                                  "id": "PVTSSF_status",
                                  "name": "Status",
                                  "options": [
                                    { "id": "option_backlog", "name": "Backlog" }
                                  ]
                                }
                              ]
                            }
                          }
                        }
                      }
                    }
                    """);
            },
            (_, _) => Task.FromResult(CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "data": {
                    "addProjectV2ItemById": {
                      "item": {
                        "id": "PVTI_anchor_org_item"
                      }
                    }
                  }
                }
                """)),
            (_, _) => Task.FromResult(CreateJsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "data": {
                    "updateProjectV2ItemFieldValue": {
                      "projectV2Item": {
                        "id": "PVTI_anchor_org_item"
                      }
                    }
                  }
                }
                """)));

        var service = CreateService(
            handler,
            new GitHubIssueOptions
            {
                Enabled = true,
                RepositoryOwner = "darkdhamon",
                RepositoryName = "The-Anchor-Bar-and-Grill",
                AccessToken = "ghs_test_token",
                ProjectOwner = "anchor-bar",
                ProjectOwnerType = nameof(GitHubProjectOwnerType.Organization),
                ProjectNumber = 7,
                ProjectStatusFieldName = "Status",
                DefaultProjectStatusName = "Backlog"
            });

        var result = await service.CreateIssueAsync(new CreateGitHubIssueRequest
        {
            Title = "Production exception",
            Body = "Stack trace and request details."
        });

        Assert.True(result.Succeeded);
        Assert.Equal(43, result.IssueNumber);
        Assert.Equal(4, handler.RequestCount);
    }

    [Fact]
    public async Task CreateIssueAsync_returns_failure_when_project_owner_type_is_invalid()
    {
        var handler = new SequenceHttpMessageHandler();
        var service = CreateService(
            handler,
            new GitHubIssueOptions
            {
                Enabled = true,
                RepositoryOwner = "darkdhamon",
                RepositoryName = "The-Anchor-Bar-and-Grill",
                AccessToken = "ghs_test_token",
                ProjectOwner = "darkdhamon",
                ProjectOwnerType = "Team",
                ProjectNumber = 7,
                ProjectStatusFieldName = "Status",
                DefaultProjectStatusName = "Backlog"
            });

        var result = await service.CreateIssueAsync(new CreateGitHubIssueRequest
        {
            Title = "Production exception",
            Body = "Stack trace and request details."
        });

        Assert.False(result.Succeeded);
        Assert.Equal(
            ["GitHub project owner type must be configured as either 'User' or 'Organization' when project placement is enabled."],
            result.Errors);
        Assert.Empty(handler.Requests);
    }

    private static GitHubIssueService CreateService(SequenceHttpMessageHandler handler, GitHubIssueOptions? options = null)
    {
        options ??= new GitHubIssueOptions
        {
            Enabled = true,
            RepositoryOwner = "darkdhamon",
            RepositoryName = "The-Anchor-Bar-and-Grill",
            AccessToken = "ghs_test_token",
            ProjectOwner = "darkdhamon",
            ProjectOwnerType = nameof(GitHubProjectOwnerType.User),
            ProjectNumber = 7,
            ProjectStatusFieldName = "Status",
            DefaultProjectStatusName = "Backlog"
        };

        return new GitHubIssueService(new HttpClient(handler), Options.Create(options));
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class SequenceHttpMessageHandler(params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] responders) : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> responders = new(responders);

        public List<HttpRequestMessage> Requests { get; } = [];

        public int RequestCount => Requests.Count;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (responders.Count == 0)
            {
                throw new InvalidOperationException("No HTTP response was configured for the request.");
            }

            return responders.Dequeue().Invoke(request, cancellationToken);
        }
    }
}
