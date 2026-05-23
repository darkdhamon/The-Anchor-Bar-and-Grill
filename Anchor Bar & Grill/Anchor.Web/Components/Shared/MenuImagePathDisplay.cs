namespace Anchor.Web.Components.Shared;

internal static class MenuImagePathDisplay
{
    public static string? Normalize(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        var trimmed = imagePath.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            return trimmed;
        }

        return trimmed.StartsWith("/", StringComparison.Ordinal)
            ? trimmed
            : $"/{trimmed.TrimStart('/')}";
    }
}
