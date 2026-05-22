namespace Anchor.Web.Issues;

public interface IProductionExceptionIssueReporter
{
    Task ReportAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken = default);
}
