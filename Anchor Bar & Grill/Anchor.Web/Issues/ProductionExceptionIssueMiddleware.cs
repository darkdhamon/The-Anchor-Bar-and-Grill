namespace Anchor.Web.Issues;

public sealed class ProductionExceptionIssueMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IProductionExceptionIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(reporter);

        EnableFormBufferingWhenUseful(context.Request);

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await reporter.ReportAsync(context, exception, context.RequestAborted);
            throw;
        }
    }

    private static void EnableFormBufferingWhenUseful(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method)
            && !HttpMethods.IsPut(request.Method)
            && !HttpMethods.IsPatch(request.Method))
        {
            return;
        }

        if (!request.HasFormContentType)
        {
            return;
        }

        request.EnableBuffering();
    }
}
