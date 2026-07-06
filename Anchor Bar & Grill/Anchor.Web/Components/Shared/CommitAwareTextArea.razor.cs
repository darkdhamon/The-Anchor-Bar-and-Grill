using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Anchor.Web.Components.Shared;

public class CommitAwareTextAreaBase : InputBase<string?>
{
    protected void HandleValueChanged(ChangeEventArgs args)
    {
        CurrentValueAsString = args.Value?.ToString() ?? string.Empty;
    }

    protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage)
    {
        result = value;
        validationErrorMessage = string.Empty;
        return true;
    }
}
