using System.Text.Json.Nodes;
using Anchor.Domain.Identity.Configuration;
using Anchor.Web.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Anchor.Web.Tests.Configuration;

public sealed class JsonConfirmedAccountConfigurationStoreTests
{
    [Fact]
    public async Task GetFallbackRequireConfirmedAccountAsync_reads_value_from_appsettings_json()
    {
        var tempPath = CreateTempDirectory();

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(tempPath, "appsettings.json"),
                """
                {
                  "AnchorIdentity": {
                    "RequireConfirmedAccount": true
                  }
                }
                """);

            var store = new JsonConfirmedAccountConfigurationStore(new TestHostEnvironment(tempPath));

            var value = await store.GetFallbackRequireConfirmedAccountAsync();

            Assert.True(value);
        }
        finally
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }

    [Fact]
    public async Task SetFallbackRequireConfirmedAccountAsync_writes_updated_value_to_appsettings_json()
    {
        var tempPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(tempPath, "appsettings.json");
            await File.WriteAllTextAsync(filePath, "{}");
            var store = new JsonConfirmedAccountConfigurationStore(new TestHostEnvironment(tempPath));

            await store.SetFallbackRequireConfirmedAccountAsync(true);

            var root = JsonNode.Parse(await File.ReadAllTextAsync(filePath))!.AsObject();
            var value = root[AnchorIdentityConfigurationKeys.SectionName]!["RequireConfirmedAccount"]!.GetValue<bool>();
            Assert.True(value);
        }
        finally
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }

    [Fact]
    public void GetEnvironmentOverride_returns_nullable_boolean_from_environment_variable()
    {
        var originalValue = Environment.GetEnvironmentVariable(AnchorIdentityConfigurationKeys.RequireConfirmedAccountEnvironmentVariable);
        var tempPath = CreateTempDirectory();
        var store = new JsonConfirmedAccountConfigurationStore(new TestHostEnvironment(tempPath));

        try
        {
            Environment.SetEnvironmentVariable(AnchorIdentityConfigurationKeys.RequireConfirmedAccountEnvironmentVariable, "true");
            Assert.True(store.GetEnvironmentOverride());

            Environment.SetEnvironmentVariable(AnchorIdentityConfigurationKeys.RequireConfirmedAccountEnvironmentVariable, "invalid");
            Assert.Null(store.GetEnvironmentOverride());
        }
        finally
        {
            Environment.SetEnvironmentVariable(AnchorIdentityConfigurationKeys.RequireConfirmedAccountEnvironmentVariable, originalValue);
            Directory.Delete(tempPath, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"anchor-web-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Anchor.Web.Tests";

        public string ContentRootPath { get; set; } = contentRootPath;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
