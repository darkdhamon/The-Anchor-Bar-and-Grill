using System.Text.RegularExpressions;
using Anchor.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Anchor.Web.Tests;

public sealed class ThemePersistenceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=AnchorThemeIntegrationTests;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    private static readonly object syncLock = new();
    private static bool databaseReady;
    private readonly WebApplicationFactory<Program> factory;

    public ThemePersistenceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", TestConnectionString);
        EnsureDatabaseReady();
        this.factory = factory;
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

    [Fact]
    public async Task AccountLogin_EmittedLocalStaticAssets_LoadSuccessfully()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var markup = await client.GetStringAsync("/Account/Login");

        var assetLinks = Regex.Matches(markup, "(?:href|src)=\"([^\"]+)\"", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value)
            .Where(value =>
                !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                (value.Contains(".css", StringComparison.OrdinalIgnoreCase) ||
                 value.Contains(".js", StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(assetLinks);

        foreach (var assetLink in assetLinks)
        {
            using var response = await client.GetAsync(assetLink);

            Assert.True(
                response.IsSuccessStatusCode,
                $"Expected static asset '{assetLink}' to load successfully, but it returned {(int)response.StatusCode}.");
        }
    }

    private static void EnsureDatabaseReady()
    {
        lock (syncLock)
        {
            if (databaseReady)
            {
                return;
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(TestConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.Migrate();
            databaseReady = true;
        }
    }
}
