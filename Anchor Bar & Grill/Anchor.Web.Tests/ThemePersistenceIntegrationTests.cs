using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Anchor.Web.Tests;

public sealed class ThemePersistenceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ThemePersistenceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=AnchorThemeIntegrationTests;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
                });
            });
        });
    }

    [Fact]
    public async Task AccountLogin_DefaultsToLightThemeWithoutCookie()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var markup = await client.GetStringAsync("/Account/Login");

        Assert.Contains("site-shell theme-light", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("site-shell theme-dark", markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountLogin_IncludesThemeBootstrapForSavedThemeSystemThemeAndTimeFallback()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var markup = await client.GetStringAsync("/Account/Login");

        Assert.Contains("anchor-theme-dark", markup, StringComparison.Ordinal);
        Assert.Contains("window.localStorage.getItem(themeKey)", markup, StringComparison.Ordinal);
        Assert.Contains("prefers-color-scheme: dark", markup, StringComparison.Ordinal);
        Assert.Contains("new Date().getHours()", markup, StringComparison.Ordinal);
        Assert.Contains("theme.js", markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountLogin_RendersBrandedAccountFormLayout()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var markup = await client.GetStringAsync("/Account/Login");

        Assert.Contains("account-page", markup, StringComparison.Ordinal);
        Assert.Contains("Secure Access", markup, StringComparison.Ordinal);
        Assert.Contains("account-form__divider", markup, StringComparison.Ordinal);
        Assert.Contains("Log in with a passkey", markup, StringComparison.Ordinal);
    }
}
