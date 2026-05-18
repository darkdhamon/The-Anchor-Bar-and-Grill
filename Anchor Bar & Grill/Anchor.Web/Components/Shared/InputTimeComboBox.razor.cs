using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Anchor.Web.Components.Shared;

public class InputTimeComboBoxBase : InputBase<string?>, IAsyncDisposable
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private IJSObjectReference? module;
    private bool isFocused;
    private bool shouldScrollMenu;
    private string? lastBoundValue;
    protected ElementReference inputRef;
    protected ElementReference menuRef;
    protected bool isOpen;
    protected string displayValue = string.Empty;
    protected IReadOnlyList<TimeOption> timeOptions = [];

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public int IncrementMinutes { get; set; } = 30;

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string ToggleAriaLabel { get; set; } = "Show time options";

    protected string ResolvedInputId { get; private set; } = $"time-combobox-{Guid.NewGuid():N}";

    protected string ListboxId => $"{ResolvedInputId}-listbox";

    protected override void OnParametersSet()
    {
        ResolvedInputId = string.IsNullOrWhiteSpace(Id)
            ? ResolvedInputId
            : Id;

        timeOptions = BuildOptions(IncrementMinutes);

        var currentBoundValue = Value ?? string.Empty;
        if (!isFocused || !string.Equals(currentBoundValue, lastBoundValue, StringComparison.Ordinal))
        {
            displayValue = FlexibleTimeText.NormalizeDisplay(currentBoundValue);
        }

        lastBoundValue = currentBoundValue;

        if (Disabled)
        {
            isOpen = false;
            shouldScrollMenu = false;
        }
    }

    protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage)
    {
        result = value;
        validationErrorMessage = string.Empty;
        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!isOpen || !shouldScrollMenu)
        {
            return;
        }

        shouldScrollMenu = false;

        try
        {
            module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Shared/InputTimeComboBox.razor.js");
            await module.InvokeVoidAsync("scrollRelevantOption", menuRef, ".time-combobox__option--active, .time-combobox__option--nearest");
        }
        catch (JSDisconnectedException)
        {
        }
    }

    protected void HandleInputFocus(FocusEventArgs _)
    {
        if (Disabled)
        {
            return;
        }

        isFocused = true;
        isOpen = true;
        shouldScrollMenu = true;
    }

    protected void HandleInputChanged(ChangeEventArgs args)
    {
        if (Disabled)
        {
            return;
        }

        displayValue = args.Value?.ToString() ?? string.Empty;
        CurrentValue = displayValue;
        isOpen = true;
        shouldScrollMenu = true;
    }

    protected async Task HandleInputBlurAsync(FocusEventArgs _)
    {
        isFocused = false;

        if (!Disabled && FlexibleTimeText.TryParse(displayValue, out var parsedTime))
        {
            displayValue = FlexibleTimeText.FormatDisplay(parsedTime);
            CurrentValue = displayValue;
        }

        isOpen = false;
        shouldScrollMenu = false;
        await Task.CompletedTask;
    }

    protected async Task HandleInputKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled)
        {
            return;
        }

        if (string.Equals(args.Key, "Escape", StringComparison.Ordinal))
        {
            isOpen = false;
            shouldScrollMenu = false;
            return;
        }

        if (string.Equals(args.Key, "Enter", StringComparison.Ordinal))
        {
            if (FlexibleTimeText.TryParse(displayValue, out var parsedTime))
            {
                displayValue = FlexibleTimeText.FormatDisplay(parsedTime);
                CurrentValue = displayValue;
            }

            isOpen = false;
            shouldScrollMenu = false;
            await inputRef.FocusAsync();
            return;
        }

        if (string.Equals(args.Key, "ArrowDown", StringComparison.Ordinal))
        {
            isOpen = true;
            shouldScrollMenu = true;
        }
    }

    protected async Task HandleToggleClickAsync()
    {
        if (Disabled)
        {
            return;
        }

        isOpen = !isOpen;
        shouldScrollMenu = isOpen;
        await inputRef.FocusAsync();
    }

    protected async Task SelectOptionAsync(TimeOption option)
    {
        displayValue = option.DisplayText;
        CurrentValue = displayValue;
        isOpen = false;
        shouldScrollMenu = false;
        await inputRef.FocusAsync();
    }

    protected string GetOptionClass(TimeOption option)
    {
        var exactOption = GetExactOption();
        if (exactOption is { CanonicalValue: var exactCanonicalValue }
            && string.Equals(option.CanonicalValue, exactCanonicalValue, StringComparison.Ordinal))
        {
            return "time-combobox__option time-combobox__option--active";
        }

        var nearestOption = GetNearestOption();
        if (nearestOption is { CanonicalValue: var nearestCanonicalValue }
            && string.Equals(option.CanonicalValue, nearestCanonicalValue, StringComparison.Ordinal))
        {
            return "time-combobox__option time-combobox__option--nearest";
        }

        return "time-combobox__option";
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null)
        {
            return;
        }

        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private TimeOption? GetExactOption()
    {
        if (!FlexibleTimeText.TryParse(displayValue, out var parsedTime))
        {
            return null;
        }

        var canonicalValue = FlexibleTimeText.FormatCanonical(parsedTime);
        return timeOptions.FirstOrDefault(option => string.Equals(option.CanonicalValue, canonicalValue, StringComparison.Ordinal));
    }

    private TimeOption? GetNearestOption()
    {
        if (!FlexibleTimeText.TryParse(displayValue, out var parsedTime))
        {
            return null;
        }

        var requestedMinutes = (parsedTime.Hour * 60) + parsedTime.Minute;
        TimeOption? closestOption = null;
        var closestDistance = int.MaxValue;

        foreach (var option in timeOptions)
        {
            var optionMinutes = (option.Time.Hour * 60) + option.Time.Minute;
            var distance = Math.Abs(optionMinutes - requestedMinutes);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestOption = option;
            }
        }

        return closestOption;
    }

    private static IReadOnlyList<TimeOption> BuildOptions(int incrementMinutes)
    {
        var safeIncrement = incrementMinutes <= 0 ? 30 : incrementMinutes;
        var options = new List<TimeOption>();

        for (var totalMinutes = 0; totalMinutes < 24 * 60; totalMinutes += safeIncrement)
        {
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            var time = new TimeOnly(hours, minutes);
            options.Add(new TimeOption(time, FlexibleTimeText.FormatDisplay(time), FlexibleTimeText.FormatCanonical(time)));
        }

        return options;
    }

    protected sealed record TimeOption(TimeOnly Time, string DisplayText, string CanonicalValue);
}
