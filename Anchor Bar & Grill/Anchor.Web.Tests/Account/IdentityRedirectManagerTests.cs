using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Tests.Account;

public sealed class IdentityRedirectManagerTests : BunitContext
{
    private const string RedirectManagerTypeName = "Anchor.Web.Components.Account.IdentityRedirectManager";
    private static readonly Type RedirectManagerType = typeof(Anchor.Web.Components.Account.Shared.RedirectToLogin).Assembly.GetType(RedirectManagerTypeName)
        ?? throw new InvalidOperationException($"Could not resolve {RedirectManagerTypeName}.");

    [Fact]
    public void RedirectTo_navigates_to_relative_uri()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var redirectManager = CreateRedirectManager(navigationManager);

        InvokeMethod(redirectManager, "RedirectTo", "Account/Login");

        Assert.Equal("http://localhost/Account/Login", navigationManager.Uri);
    }

    [Fact]
    public void RedirectTo_normalizes_same_host_absolute_uri_to_relative_navigation()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var redirectManager = CreateRedirectManager(navigationManager);

        InvokeMethod(redirectManager, "RedirectTo", "http://localhost/admin/users?filter=pending");

        Assert.Equal("http://localhost/admin/users?filter=pending", navigationManager.Uri);
    }

    [Fact]
    public void RedirectTo_rejects_external_absolute_uri()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var redirectManager = CreateRedirectManager(navigationManager);

        var exception = Assert.Throws<TargetInvocationException>(() =>
            InvokeMethod(redirectManager, "RedirectTo", "https://evil.example/phish"));

        Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Equal("http://localhost/", navigationManager.Uri);
    }

    [Fact]
    public void RedirectToWithStatus_sets_short_lived_status_cookie_and_redirects()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var redirectManager = CreateRedirectManager(navigationManager);
        var httpContext = new DefaultHttpContext();

        InvokeMethod(redirectManager, "RedirectToWithStatus", "Account/Login", "saved", httpContext);

        Assert.Equal("http://localhost/Account/Login", navigationManager.Uri);

        var setCookie = Assert.Single(httpContext.Response.Headers.SetCookie);
        Assert.Contains("Identity.StatusMessage=saved", setCookie, StringComparison.Ordinal);
        Assert.Contains("path=/", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=5", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    private static object CreateRedirectManager(NavigationManager navigationManager) =>
        Activator.CreateInstance(RedirectManagerType, navigationManager)
        ?? throw new InvalidOperationException("Could not create IdentityRedirectManager.");

    private static object? InvokeMethod(object instance, string methodName, params object?[] arguments)
    {
        var argumentTypes = arguments
            .Select(argument => argument?.GetType() ?? typeof(object))
            .ToArray();

        var method = RedirectManagerType.GetMethod(methodName, argumentTypes)
            ?? throw new InvalidOperationException($"Could not resolve method {methodName}.");

        return method.Invoke(instance, arguments);
    }
}
