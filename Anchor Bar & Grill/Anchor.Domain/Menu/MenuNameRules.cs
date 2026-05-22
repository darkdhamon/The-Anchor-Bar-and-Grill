namespace Anchor.Domain.Menu;

public static class MenuNameRules
{
    public static string NormalizeForLookup(string value) => value.Trim().ToUpperInvariant();
}
