using System.Security.Claims;
using Anchor.Domain.Issues;
using Anchor.Web.Issues;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Anchor.Web.Tests.Issues;

public sealed class ProductionExceptionIssueReporterTests
{
    [Fact]
    public async Task ReportAsync_creates_issue_for_production_requests_and_redacts_sensitive_request_values()
    {
        var gitHubIssueService = new FakeGitHubIssueService();
        var reporter = CreateReporter(gitHubIssueService);
        var context = CreateHttpContext("anchorbarandgrill.com");
        context.TraceIdentifier = "trace-123";
        context.Request.Method = HttpMethods.Post;
        context.Request.Scheme = "https";
        context.Request.Path = "/Account/Login";
        context.Request.QueryString = new QueryString("?returnUrl=%2Fadmin&password=secret");
        context.Request.Headers.Referer = "https://anchorbarandgrill.com/login";
        context.Request.Headers.UserAgent = "Anchor Browser";
        context.Request.Headers["X-Correlation-ID"] = "corr-123";
        context.Request.RouteValues["area"] = "Account";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = new MemoryStream("email=staff%40anchor.com&password=supersecret&__RequestVerificationToken=abc123"u8.ToArray());
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-42"),
            new Claim(ClaimTypes.Name, "captain@anchorbarandgrill.com"),
            new Claim(ClaimTypes.Role, "Admin")
        ],
        "TestAuth"));

        await reporter.ReportAsync(context, new InvalidOperationException("Unexpected login failure."));

        var request = Assert.Single(gitHubIssueService.Requests);
        Assert.Equal("Backlog", request.ProjectStatusName);
        Assert.Contains("bug", request.Labels);
        Assert.Contains("Production Exception: InvalidOperationException on POST /Account/Login", request.Title, StringComparison.Ordinal);
        Assert.Contains("Unexpected login failure.", request.Body, StringComparison.Ordinal);
        Assert.Contains("`password`: `[REDACTED]`", request.Body, StringComparison.Ordinal);
        Assert.Contains("`__RequestVerificationToken`: `[REDACTED]`", request.Body, StringComparison.Ordinal);
        Assert.Contains("`returnUrl`: `/admin`", request.Body, StringComparison.Ordinal);
        Assert.Contains("User ID: `user-42`", request.Body, StringComparison.Ordinal);
        Assert.Contains("Roles: `Admin`", request.Body, StringComparison.Ordinal);
        Assert.Contains("Trace Identifier: `trace-123`", request.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("supersecret", request.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("abc123", request.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("password=secret", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportAsync_skips_localhost_and_non_production_requests()
    {
        var gitHubIssueService = new FakeGitHubIssueService();
        var productionReporter = CreateReporter(gitHubIssueService);
        var localhostContext = CreateHttpContext("localhost");

        await productionReporter.ReportAsync(localhostContext, new InvalidOperationException("Local failure."));

        var stagingReporter = CreateReporter(gitHubIssueService, environmentName: Environments.Staging);
        var stagingContext = CreateHttpContext("anchorbarandgrill.com");

        await stagingReporter.ReportAsync(stagingContext, new InvalidOperationException("Staging failure."));

        Assert.Empty(gitHubIssueService.Requests);
    }

    [Fact]
    public async Task ReportAsync_suppresses_duplicate_exceptions_after_a_successful_issue_creation()
    {
        var gitHubIssueService = new FakeGitHubIssueService();
        var reporter = CreateReporter(gitHubIssueService);
        var context = CreateHttpContext("anchorbarandgrill.com");
        var exception = new InvalidOperationException("Repeated failure.");

        await reporter.ReportAsync(context, exception);
        await reporter.ReportAsync(context, exception);

        Assert.Single(gitHubIssueService.Requests);
    }

    [Fact]
    public async Task ReportAsync_does_not_suppress_retries_when_issue_creation_fails()
    {
        var gitHubIssueService = new FakeGitHubIssueService
        {
            NextResult = GitHubIssueCreationResult.Failure("GitHub unavailable.")
        };
        var reporter = CreateReporter(gitHubIssueService);
        var context = CreateHttpContext("anchorbarandgrill.com");
        var exception = new InvalidOperationException("Repeated failure.");

        await reporter.ReportAsync(context, exception);
        await reporter.ReportAsync(context, exception);

        Assert.Equal(2, gitHubIssueService.Requests.Count);
    }

    private static ProductionExceptionIssueReporter CreateReporter(
        FakeGitHubIssueService gitHubIssueService,
        ProductionExceptionIssueOptions? options = null,
        string environmentName = "Production")
    {
        options ??= new ProductionExceptionIssueOptions
        {
            Enabled = true,
            Labels = ["bug"],
            ProjectStatusName = "Backlog",
            DuplicateSuppressionWindowMinutes = 15
        };

        return new ProductionExceptionIssueReporter(
            gitHubIssueService,
            new MemoryCache(new MemoryCacheOptions()),
            new TestHostEnvironment(environmentName),
            new FakeOptionsMonitor<ProductionExceptionIssueOptions>(options),
            NullLogger<ProductionExceptionIssueReporter>.Instance);
    }

    private static DefaultHttpContext CreateHttpContext(string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString(host);
        context.Request.Path = "/";
        context.RequestServices = new ServiceCollection().BuildServiceProvider();
        return context;
    }

    private sealed class FakeGitHubIssueService : IGitHubIssueService
    {
        public List<CreateGitHubIssueRequest> Requests { get; } = [];

        public GitHubIssueCreationResult NextResult { get; set; } = GitHubIssueCreationResult.Success(100, "https://github.com/darkdhamon/The-Anchor-Bar-and-Grill/issues/100");

        public Task<GitHubIssueCreationResult> CreateIssueAsync(CreateGitHubIssueRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(NextResult);
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Anchor.Web.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue => currentValue;

        public T Get(string? name) => currentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
