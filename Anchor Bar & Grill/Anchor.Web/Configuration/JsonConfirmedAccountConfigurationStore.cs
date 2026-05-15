using System.Text.Json;
using System.Text.Json.Nodes;
using Anchor.Domain.Identity.Configuration;

namespace Anchor.Web.Configuration;

public sealed class JsonConfirmedAccountConfigurationStore(IHostEnvironment environment) : IConfirmedAccountConfigurationStore
{
    private readonly string appsettingsPath = Path.Combine(environment.ContentRootPath, "appsettings.json");

    public bool? GetEnvironmentOverride()
    {
        var rawValue = Environment.GetEnvironmentVariable(AnchorIdentityConfigurationKeys.RequireConfirmedAccountEnvironmentVariable);
        return bool.TryParse(rawValue, out var parsedValue) ? parsedValue : null;
    }

    public async Task<bool> GetFallbackRequireConfirmedAccountAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(appsettingsPath))
        {
            return false;
        }

        await using var stream = File.OpenRead(appsettingsPath);
        var rootNode = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken) as JsonObject;
        var anchorIdentitySection = rootNode?[AnchorIdentityConfigurationKeys.SectionName] as JsonObject;

        return anchorIdentitySection?["RequireConfirmedAccount"]?.GetValue<bool>() ?? false;
    }

    public async Task SetFallbackRequireConfirmedAccountAsync(bool requireConfirmedAccount, CancellationToken cancellationToken = default)
    {
        JsonObject rootNode;

        if (File.Exists(appsettingsPath))
        {
            await using var readStream = File.OpenRead(appsettingsPath);
            rootNode = (await JsonNode.ParseAsync(readStream, cancellationToken: cancellationToken) as JsonObject) ?? new JsonObject();
        }
        else
        {
            rootNode = new JsonObject();
        }

        var anchorIdentitySection = rootNode[AnchorIdentityConfigurationKeys.SectionName] as JsonObject ?? new JsonObject();
        anchorIdentitySection["RequireConfirmedAccount"] = requireConfirmedAccount;
        rootNode[AnchorIdentityConfigurationKeys.SectionName] = anchorIdentitySection;

        await using var writeStream = File.Create(appsettingsPath);
        await JsonSerializer.SerializeAsync(writeStream, rootNode, new JsonSerializerOptions
        {
            WriteIndented = true
        }, cancellationToken);
    }
}
