using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace Anchor.Web.Components.Shared;

public class CommitAwareInputTextBase : InputBase<string?>
{
    [Parameter]
    public string Type { get; set; } = "text";

    [Parameter]
    public EventCallback<FocusEventArgs> OnBlur { get; set; }

    protected void HandleValueChanged(ChangeEventArgs args)
    {
        CurrentValueAsString = args.Value?.ToString() ?? string.Empty;
    }

    protected Task HandleBlurAsync(FocusEventArgs args) => OnBlur.InvokeAsync(args);

    protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage)
    {
        result = value;
        validationErrorMessage = string.Empty;
        return true;
    }
}
