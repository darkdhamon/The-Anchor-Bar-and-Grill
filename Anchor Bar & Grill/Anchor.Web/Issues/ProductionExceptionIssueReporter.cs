using System.Net;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Anchor.Domain.Issues;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Anchor.Web.Issues;

public sealed class ProductionExceptionIssueReporter(
    IGitHubIssueService gitHubIssueService,
    IMemoryCache memoryCache,
    IHostEnvironment hostEnvironment,
    IOptionsMonitor<ProductionExceptionIssueOptions> optionsMonitor,
    ILogger<ProductionExceptionIssueReporter> logger) : IProductionExceptionIssueReporter
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "pwd",
        "secret",
        "token",
        "auth",
        "cookie",
        "session",
        "apikey",
        "api-key",
        "privatekey",
        "private-key",
        "accesskey",
        "access-key",
        "connectionstring",
        "__requestverificationtoken",
        "antiforgery"
    ];

    private static readonly string[] SafeHeaderNames =
    [
        "Content-Type",
        "Origin",
        "Referer",
        "User-Agent",
        "X-Forwarded-For",
        "X-Forwarded-Host",
        "X-Forwarded-Proto",
        "X-Request-ID",
        "X-Correlation-ID"
    ];

    public async Task ReportAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var options = optionsMonitor.CurrentValue;
        if (!ShouldReport(httpContext, options))
        {
            return;
        }

        var fingerprint = CreateFingerprint(httpContext, exception);
        if (IsDuplicate(fingerprint, options))
        {
            logger.LogInformation(
                "Skipped duplicate automated GitHub exception issue for trace {TraceIdentifier}.",
                httpContext.TraceIdentifier);
            return;
        }

        CreateGitHubIssueRequest issueRequest;

        try
        {
            issueRequest = await BuildIssueRequestAsync(httpContext, exception, options, cancellationToken);
        }
        catch (Exception snapshotException)
        {
            logger.LogError(
                snapshotException,
                "Unable to build the automated GitHub exception issue payload for trace {TraceIdentifier}.",
                httpContext.TraceIdentifier);
            return;
        }

        GitHubIssueCreationResult result;

        try
        {
            result = await gitHubIssueService.CreateIssueAsync(issueRequest, cancellationToken);
        }
        catch (Exception reportException)
        {
            logger.LogError(
                reportException,
                "Automated GitHub exception issue creation threw for trace {TraceIdentifier}.",
                httpContext.TraceIdentifier);
            return;
        }

        if (!result.Succeeded)
        {
            logger.LogError(
                "Automated GitHub exception issue creation failed for trace {TraceIdentifier}: {Errors}",
                httpContext.TraceIdentifier,
                string.Join(" | ", result.Errors));
            return;
        }

        CacheFingerprint(fingerprint, options);

        logger.LogInformation(
            "Created automated GitHub exception issue #{IssueNumber} for trace {TraceIdentifier}.",
            result.IssueNumber,
            httpContext.TraceIdentifier);
    }

    private bool ShouldReport(HttpContext httpContext, ProductionExceptionIssueOptions options)
    {
        if (!options.Enabled)
        {
            return false;
        }

        if (!hostEnvironment.IsProduction())
        {
            return false;
        }

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return true;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !IPAddress.TryParse(host, out var address) || !IPAddress.IsLoopback(address);
    }

    private bool IsDuplicate(string fingerprint, ProductionExceptionIssueOptions options)
    {
        if (options.DuplicateSuppressionWindowMinutes <= 0)
        {
            return false;
        }

        return memoryCache.TryGetValue(fingerprint, out _);
    }

    private void CacheFingerprint(string fingerprint, ProductionExceptionIssueOptions options)
    {
        if (options.DuplicateSuppressionWindowMinutes <= 0)
        {
            return;
        }

        memoryCache.Set(
            fingerprint,
            true,
            TimeSpan.FromMinutes(options.DuplicateSuppressionWindowMinutes));
    }

    private async Task<CreateGitHubIssueRequest> BuildIssueRequestAsync(
        HttpContext httpContext,
        Exception exception,
        ProductionExceptionIssueOptions options,
        CancellationToken cancellationToken)
    {
        var request = httpContext.Request;
        var routeValues = FormatKeyValuePairs(request.RouteValues.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value?.ToString())));
        var queryValues = FormatKeyValuePairs(request.Query.SelectMany(pair => pair.Value, (pair, value) => new KeyValuePair<string, string?>(pair.Key, value)));
        var headerValues = FormatKeyValuePairs(GetSafeHeaders(request));
        var formData = await BuildFormDataSectionAsync(request, cancellationToken);
        var userContext = BuildUserContextSection(httpContext.User);
        var requestUrl = BuildSanitizedRequestUrl(request);
        var sanitizedQueryString = BuildSanitizedQueryString(request.Query);
        var exceptionDetails = exception.ToString();
        var requestTimestamp = DateTimeOffset.UtcNow;

        var title = BuildTitle(options.TitlePrefix, exception, request);
        var body = BuildBody(
            httpContext,
            requestTimestamp,
            exceptionDetails,
            requestUrl,
            sanitizedQueryString,
            routeValues,
            queryValues,
            formData,
            headerValues,
            userContext);

        return new CreateGitHubIssueRequest
        {
            Title = title,
            Body = body,
            Labels = options.Labels,
            AddToConfiguredProject = true,
            ProjectStatusName = options.ProjectStatusName
        };
    }

    private static string BuildTitle(string titlePrefix, Exception exception, HttpRequest request)
    {
        var prefix = string.IsNullOrWhiteSpace(titlePrefix) ? "Production Exception" : titlePrefix.Trim();
        var path = request.Path.HasValue ? request.Path.Value : "/";
        var title = $"{prefix}: {exception.GetType().Name} on {request.Method} {path}";
        return title.Length <= 240 ? title : title[..240];
    }

    private static string BuildBody(
        HttpContext httpContext,
        DateTimeOffset requestTimestamp,
        string exceptionDetails,
        string requestUrl,
        string sanitizedQueryString,
        string routeValues,
        string queryValues,
        string formData,
        string headerValues,
        string userContext)
    {
        var request = httpContext.Request;
        var endpointDisplayName = httpContext.GetEndpoint()?.DisplayName ?? "(none)";
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "(unknown)";
        var contentLength = request.ContentLength?.ToString() ?? "(unknown)";
        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "(none)" : request.ContentType;
        var correlationId = GetHeaderValue(request, "X-Correlation-ID");
        var requestId = GetHeaderValue(request, "X-Request-ID");

        var builder = new StringBuilder();
        builder.AppendLine("## Summary");
        builder.AppendLine("This issue was created automatically from an unhandled production exception.");
        builder.AppendLine();
        builder.AppendLine("## Request Overview");
        builder.AppendLine($"- Occurred (UTC): `{requestTimestamp:O}`");
        builder.AppendLine($"- Trace Identifier: `{httpContext.TraceIdentifier}`");
        builder.AppendLine($"- Request ID Header: `{requestId}`");
        builder.AppendLine($"- Correlation ID Header: `{correlationId}`");
        builder.AppendLine($"- Method: `{request.Method}`");
        builder.AppendLine($"- URL: `{requestUrl}`");
        builder.AppendLine($"- Scheme: `{request.Scheme}`");
        builder.AppendLine($"- Host: `{request.Host}`");
        builder.AppendLine($"- Path Base: `{request.PathBase}`");
        builder.AppendLine($"- Path: `{request.Path}`");
        builder.AppendLine($"- Query String: `{sanitizedQueryString}`");
        builder.AppendLine($"- Endpoint: `{endpointDisplayName}`");
        builder.AppendLine($"- Remote IP: `{remoteIp}`");
        builder.AppendLine($"- Content Type: `{contentType}`");
        builder.AppendLine($"- Content Length: `{contentLength}`");
        builder.AppendLine();
        builder.AppendLine("## User Context");
        builder.AppendLine(userContext);
        builder.AppendLine();
        builder.AppendLine("## Route Values");
        builder.AppendLine(routeValues);
        builder.AppendLine();
        builder.AppendLine("## Query Values");
        builder.AppendLine(queryValues);
        builder.AppendLine();
        builder.AppendLine("## Form Data");
        builder.AppendLine(formData);
        builder.AppendLine();
        builder.AppendLine("## Selected Headers");
        builder.AppendLine(headerValues);
        builder.AppendLine();
        builder.AppendLine("## Exception");
        builder.AppendLine("```text");
        builder.AppendLine(exceptionDetails);
        builder.AppendLine("```");

        return builder.ToString().TrimEnd();
    }

    private async Task<string> BuildFormDataSectionAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return "_No form data was submitted with this request._";
        }

        try
        {
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }

            var form = await request.ReadFormAsync(cancellationToken);

            var builder = new StringBuilder();
            if (form.Count == 0 && form.Files.Count == 0)
            {
                builder.AppendLine("_The request had form content type, but no form fields were available._");
            }
            else
            {
                foreach (var pair in form.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    foreach (var value in pair.Value)
                    {
                        builder.AppendLine($"- `{pair.Key}`: `{RedactIfNeeded(pair.Key, value)}`");
                    }
                }

                foreach (var file in form.Files.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
                {
                    builder.AppendLine($"- File `{file.Name}`: `{SanitizeText(file.FileName)}` ({file.Length} bytes, `{file.ContentType}`)");
                }
            }

            return builder.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"_Form data could not be read safely after the exception: {SanitizeText(ex.Message)}_";
        }
        finally
        {
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }
        }
    }

    private static string BuildUserContextSection(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return "_Request was unauthenticated._";
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "(unknown)";
        var userName = user.Identity.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? "(unknown)";
        var roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        var builder = new StringBuilder();
        builder.AppendLine($"- User ID: `{SanitizeText(userId)}`");
        builder.AppendLine($"- User Name: `{SanitizeText(userName)}`");
        builder.AppendLine($"- Roles: `{(roles.Length == 0 ? "(none)" : string.Join(", ", roles.Select(SanitizeText)))}`");
        return builder.ToString().TrimEnd();
    }

    private static IEnumerable<KeyValuePair<string, string?>> GetSafeHeaders(HttpRequest request)
    {
        foreach (var headerName in SafeHeaderNames)
        {
            if (!request.Headers.TryGetValue(headerName, out var values))
            {
                continue;
            }

            foreach (var value in values)
            {
                yield return new KeyValuePair<string, string?>(headerName, value);
            }
        }
    }

    private static string BuildSanitizedRequestUrl(HttpRequest request)
    {
        var builder = new StringBuilder();
        builder.Append(request.Scheme);
        builder.Append("://");
        builder.Append(request.Host);
        builder.Append(request.PathBase);
        builder.Append(request.Path);

        var queryString = BuildSanitizedQueryString(request.Query);
        if (!string.Equals(queryString, "(empty)", StringComparison.Ordinal))
        {
            builder.Append(queryString);
        }

        return builder.ToString();
    }

    private static string BuildSanitizedQueryString(IQueryCollection query)
    {
        if (query.Count == 0)
        {
            return "(empty)";
        }

        var segments = new List<string>();
        foreach (var pair in query.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var value in pair.Value)
            {
                var sanitizedValue = RedactIfNeeded(pair.Key, value);
                segments.Add($"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(sanitizedValue)}");
            }
        }

        return $"?{string.Join("&", segments)}";
    }

    private static string FormatKeyValuePairs(IEnumerable<KeyValuePair<string, string?>> pairs)
    {
        var entries = pairs
            .Select(pair => new KeyValuePair<string, string?>(pair.Key, RedactIfNeeded(pair.Key, pair.Value)))
            .ToArray();

        if (entries.Length == 0)
        {
            return "_None._";
        }

        var builder = new StringBuilder();
        foreach (var pair in entries.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- `{pair.Key}`: `{SanitizeText(pair.Value)}`");
        }

        return builder.ToString().TrimEnd();
    }

    private static string CreateFingerprint(HttpContext httpContext, Exception exception)
    {
        var path = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : "/";
        var firstStackLine = exception.StackTrace?
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()
            ?? exception.TargetSite?.ToString()
            ?? "(none)";

        var rawFingerprint = $"{exception.GetType().FullName}|{exception.Message}|{path}|{firstStackLine}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawFingerprint));
        return Convert.ToHexString(hashBytes);
    }

    private static string GetHeaderValue(HttpRequest request, string headerName) =>
        request.Headers.TryGetValue(headerName, out var values)
            ? SanitizeText(values.ToString())
            : "(none)";

    private static string RedactIfNeeded(string key, string? value)
    {
        if (IsSensitiveKey(key))
        {
            return "[REDACTED]";
        }

        return SanitizePotentialUrl(value);
    }

    private static bool IsSensitiveKey(string key) =>
        SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static string SanitizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(empty)";
        }

        return value
            .Replace("`", "'")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }

    private static string SanitizePotentialUrl(string? value)
    {
        var sanitized = SanitizeText(value);
        if (sanitized is "(empty)")
        {
            return sanitized;
        }

        if (!Uri.TryCreate(sanitized, UriKind.Absolute, out var uri))
        {
            return sanitized;
        }

        if (string.IsNullOrEmpty(uri.Query))
        {
            return sanitized;
        }

        var queryValues = QueryHelpers.ParseQuery(uri.Query);
        var builder = new StringBuilder();
        builder.Append(uri.GetLeftPart(UriPartial.Path));

        if (queryValues.Count > 0)
        {
            var segments = new List<string>();
            foreach (var pair in queryValues.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var queryValue in pair.Value)
                {
                    var sanitizedValue = RedactIfNeeded(pair.Key, queryValue);
                    segments.Add($"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(sanitizedValue)}");
                }
            }

            builder.Append('?');
            builder.Append(string.Join("&", segments));
        }

        return builder.ToString();
    }
}
