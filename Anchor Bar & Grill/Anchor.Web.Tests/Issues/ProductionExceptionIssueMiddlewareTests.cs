using Microsoft.AspNetCore.Http;
using Anchor.Web.Issues;

namespace Anchor.Web.Tests.Issues;

public sealed class ProductionExceptionIssueMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_reports_exception_and_rethrows_it()
    {
        var reporter = new FakeProductionExceptionIssueReporter();
        var middleware = new ProductionExceptionIssueMiddleware(_ => throw new InvalidOperationException("Boom."));
        var context = new DefaultHttpContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context, reporter));

        Assert.Equal("Boom.", exception.Message);
        Assert.Same(context, reporter.Context);
        Assert.IsType<InvalidOperationException>(reporter.Exception);
    }

    [Fact]
    public async Task InvokeAsync_enables_form_buffering_for_form_posts_before_running_next()
    {
        var middleware = new ProductionExceptionIssueMiddleware(_ =>
        {
            Assert.True(_.Request.Body.CanSeek);
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = new MemoryStream("name=Anchor"u8.ToArray());

        await middleware.InvokeAsync(context, new FakeProductionExceptionIssueReporter());

        Assert.True(context.Request.Body.CanSeek);
    }

    private sealed class FakeProductionExceptionIssueReporter : IProductionExceptionIssueReporter
    {
        public HttpContext? Context { get; private set; }

        public Exception? Exception { get; private set; }

        public Task ReportAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken = default)
        {
            Context = httpContext;
            Exception = exception;
            return Task.CompletedTask;
        }
    }
}
