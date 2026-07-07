using System.Globalization;
using Anchor.Domain.Events;
using Anchor.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Anchor.Web.Components.Pages.Admin;

public partial class EventsAdmin
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly IReadOnlyList<EventPublicationState> publicationStates =
    [
        EventPublicationState.Draft,
        EventPublicationState.Published,
        EventPublicationState.Archived
    ];
    private static readonly IReadOnlyList<EventRecurrencePattern> recurrencePatterns =
    [
        EventRecurrencePattern.None,
        EventRecurrencePattern.Weekly,
        EventRecurrencePattern.MonthlyNthWeekday
    ];
    private static readonly IReadOnlyList<EventRecurrenceWeek> recurrenceWeekOptions =
    [
        EventRecurrenceWeek.First,
        EventRecurrenceWeek.Second,
        EventRecurrenceWeek.Third,
        EventRecurrenceWeek.Fourth,
        EventRecurrenceWeek.Last
    ];
    private static readonly IReadOnlyList<DayOfWeek> recurringEventDayOptions =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    private readonly List<string> validationErrors = [];
    private List<EventRecord> eventRecords = [];
    private EventEditorFormModel form = new();
    private string? statusMessage;
    private bool isLoading = true;
    private bool isSaving;
    private bool isDeleting;
    private Guid? pendingDeleteId;

    [Inject]
    private IEventManagementService EventManagementService { get; set; } = null!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = null!;

    private IReadOnlyList<string> eventBadgeOptions =>
        eventRecords
            .Select(item => item.PromoBadge)
            .Where(badge => !string.IsNullOrWhiteSpace(badge))
            .Select(badge => badge!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(badge => badge, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private bool IsRecurring => form.RecurrencePattern != EventRecurrencePattern.None;

    private bool IsMonthlyRecurrence => form.RecurrencePattern == EventRecurrencePattern.MonthlyNthWeekday;

    private int CurrentRecurrenceIntervalLimit =>
        form.RecurrencePattern switch
        {
            EventRecurrencePattern.Weekly => EventScheduleRules.MaxWeeklyRecurrenceInterval,
            EventRecurrencePattern.MonthlyNthWeekday => EventScheduleRules.MaxMonthlyRecurrenceInterval,
            _ => 1
        };

    private string RecurrenceIntervalHelpText =>
        form.RecurrencePattern switch
        {
            EventRecurrencePattern.Weekly => $"Weekly schedules can repeat up to every {EventScheduleRules.MaxWeeklyRecurrenceInterval} weeks.",
            EventRecurrencePattern.MonthlyNthWeekday => $"Monthly nth-weekday schedules can repeat up to every {EventScheduleRules.MaxMonthlyRecurrenceInterval} months.",
            _ => "Choose a recurring pattern to enable cadence controls."
        };

    private string EditorHeading =>
        form.EventId is null
            ? "Add event"
            : $"Edit event: {form.Title}";

    private string EditorLead =>
        form.EventId is null
            ? "Create a new event definition, then decide whether to keep it as a draft or publish it immediately."
            : "Update the current event details here. Saving this form updates the existing event definition instead of creating a duplicate.";

    private string EditorBadge =>
        form.EventId is null
            ? "New event"
            : GetPublicationLabel(form.PublicationState);

    private bool IsMutating => isSaving || isDeleting;

    private bool EventActionsDisabled => isLoading || IsMutating;

    private string SaveButtonText => isSaving ? "Saving" : "Save event";

    protected override async Task OnInitializedAsync()
    {
        await LoadEventsAsync(resetToNewEvent: true);
    }

    private async Task LoadEventsAsync(Guid? selectEventId = null, bool resetToNewEvent = false)
    {
        isLoading = true;
        eventRecords = (await EventManagementService.GetEventsAsync()).ToList();
        isLoading = false;

        if (selectEventId is Guid requestedEventId)
        {
            var selectedEvent = eventRecords.SingleOrDefault(item => item.EventId == requestedEventId);
            if (selectedEvent is not null)
            {
                LoadEditor(selectedEvent);
                return;
            }
        }

        if (!resetToNewEvent && form.EventId is Guid currentEventId)
        {
            var currentEvent = eventRecords.SingleOrDefault(item => item.EventId == currentEventId);
            if (currentEvent is not null)
            {
                LoadEditor(currentEvent);
                return;
            }
        }

        StartNewEvent();
    }

    private void StartNewEvent()
    {
        var today = DateOnly.FromDateTime(TimeProvider.GetLocalNow().DateTime);
        form = new EventEditorFormModel
        {
            StartsOnText = FormatDate(today),
            StartsAtText = FlexibleTimeText.FormatDisplay(new TimeOnly(19, 0)),
            SortOrder = eventRecords.Select(item => item.SortOrder).DefaultIfEmpty(0).Max() + 1,
            PublicationState = EventPublicationState.Draft,
            RecurrencePattern = EventRecurrencePattern.None,
            RecurrenceInterval = 1,
            RecursOnDayOfWeek = today.DayOfWeek,
            RecursOnWeekOfMonth = GetDefaultWeekOfMonth(today)
        };

        validationErrors.Clear();
        pendingDeleteId = null;
    }

    private void ResetEditor()
    {
        if (IsMutating)
        {
            return;
        }

        validationErrors.Clear();
        pendingDeleteId = null;

        if (form.EventId is Guid eventId)
        {
            var selectedEvent = eventRecords.SingleOrDefault(item => item.EventId == eventId);
            if (selectedEvent is not null)
            {
                LoadEditor(selectedEvent);
                return;
            }
        }

        StartNewEvent();
    }

    private void EditEvent(Guid eventId)
    {
        if (IsMutating)
        {
            return;
        }

        var selectedEvent = eventRecords.SingleOrDefault(item => item.EventId == eventId);
        if (selectedEvent is null)
        {
            statusMessage = "Error: The requested event could not be loaded.";
            return;
        }

        statusMessage = null;
        LoadEditor(selectedEvent);
    }

    private void LoadEditor(EventRecord record)
    {
        form = new EventEditorFormModel
        {
            EventId = record.EventId,
            Title = record.Title,
            Summary = record.Summary,
            Description = record.Description,
            PromoBadge = record.PromoBadge,
            ImagePath = record.ImagePath,
            StartsOnText = FormatDate(record.StartsOn),
            StartsAtText = FormatTime(record.StartsAt),
            EndsAtText = FormatTime(record.EndsAt),
            EndsNextDay = record.EndsNextDay,
            SortOrder = record.SortOrder,
            PublicationState = record.PublicationState,
            RecurrencePattern = record.RecurrencePattern,
            RecurrenceInterval = record.RecurrencePattern == EventRecurrencePattern.None ? 1 : record.RecurrenceInterval,
            RecursOnDayOfWeek = record.RecursOnDayOfWeek ?? record.StartsOn.DayOfWeek,
            RecursOnWeekOfMonth = record.RecursOnWeekOfMonth ?? GetDefaultWeekOfMonth(record.StartsOn),
            RecursUntilText = FormatDate(record.RecursUntil),
            TimingNotes = record.TimingNotes
        };

        validationErrors.Clear();
        pendingDeleteId = null;
    }

    private async Task SaveAsync() =>
        await SaveEventAsync(form.PublicationState);

    private async Task SaveDraftAsync() =>
        await SaveEventAsync(EventPublicationState.Draft);

    private async Task PublishAsync() =>
        await SaveEventAsync(EventPublicationState.Published);

    private async Task ArchiveAsync()
    {
        if (form.EventId is null)
        {
            return;
        }

        await SaveEventAsync(EventPublicationState.Archived);
    }

    private async Task SaveEventAsync(EventPublicationState publicationState)
    {
        if (IsMutating)
        {
            return;
        }

        isSaving = true;
        validationErrors.Clear();
        pendingDeleteId = null;
        statusMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            if (!TryBuildSaveRequest(publicationState, out var request))
            {
                statusMessage = "Error: Fix the event details below and try again.";
                return;
            }

            var wasNewRecord = request!.EventId is null;
            var result = await EventManagementService.SaveEventAsync(request);
            if (!result.Succeeded)
            {
                validationErrors.AddRange(result.Errors);
                statusMessage = "Error: The event could not be saved.";
                return;
            }

            statusMessage = GetSaveStatusMessage(publicationState, wasNewRecord);
            await LoadEventsAsync(result.EventId, resetToNewEvent: false);
        }
        finally
        {
            isSaving = false;
        }
    }

    private bool TryBuildSaveRequest(EventPublicationState publicationState, out SaveEventRequest? request)
    {
        request = null;

        if (!TryParseRequiredDate(form.StartsOnText, "Event date", out var startsOn)
            | !TryParseRequiredTime(form.StartsAtText, "Start time", out var startsAt)
            | !TryParseOptionalTime(form.EndsAtText, "End time", out var endsAt)
            | !TryParseOptionalDate(form.RecursUntilText, "Recurs until", out var recursUntil))
        {
            return false;
        }

        var recurrencePattern = form.RecurrencePattern;
        var recurrenceInterval = recurrencePattern == EventRecurrencePattern.None
            ? 1
            : Math.Max(1, form.RecurrenceInterval);

        request = new SaveEventRequest(
            form.EventId,
            form.Title,
            form.Summary,
            form.Description,
            NormalizeOptionalValue(form.PromoBadge),
            NormalizeOptionalValue(form.ImagePath),
            startsOn!.Value,
            startsAt!.Value,
            endsAt,
            form.EndsNextDay,
            Math.Max(1, form.SortOrder),
            publicationState,
            recurrencePattern,
            recurrenceInterval,
            recurrencePattern == EventRecurrencePattern.None ? null : form.RecursOnDayOfWeek,
            recurrencePattern == EventRecurrencePattern.MonthlyNthWeekday ? form.RecursOnWeekOfMonth : null,
            recurrencePattern == EventRecurrencePattern.None ? null : recursUntil,
            NormalizeOptionalValue(form.TimingNotes));

        return true;
    }

    private void RequestDelete(Guid eventId)
    {
        if (IsMutating)
        {
            return;
        }

        pendingDeleteId = eventId;
        validationErrors.Clear();
        statusMessage = null;
    }

    private void CancelDelete()
    {
        if (IsMutating)
        {
            return;
        }

        pendingDeleteId = null;
    }

    private async Task ConfirmDeleteAsync(Guid eventId)
    {
        if (IsMutating)
        {
            return;
        }

        isDeleting = true;
        validationErrors.Clear();
        pendingDeleteId = null;
        statusMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await EventManagementService.DeleteEventAsync(eventId);
            if (!result.Succeeded)
            {
                validationErrors.AddRange(result.Errors);
                statusMessage = "Error: The event could not be deleted.";
                return;
            }

            var preservedSelection = form.EventId == eventId ? null : form.EventId;
            statusMessage = "Event deleted.";
            await LoadEventsAsync(preservedSelection, resetToNewEvent: preservedSelection is null);
        }
        finally
        {
            isDeleting = false;
        }
    }

    private void HandleStartsOnChanged(ChangeEventArgs args)
    {
        DateOnly? previousDate = TryParseDateText(form.StartsOnText, out var parsedPreviousDate)
            ? parsedPreviousDate
            : null;

        form.StartsOnText = args.Value?.ToString() ?? string.Empty;

        if (form.EventId is not null
            || !TryParseDateText(form.StartsOnText, out var selectedDate))
        {
            return;
        }

        var shouldSyncRecurrenceDay = previousDate is null || form.RecursOnDayOfWeek == previousDate.Value.DayOfWeek;
        var previousWeekOfMonth = previousDate is { } priorDate ? GetDefaultWeekOfMonth(priorDate) : (EventRecurrenceWeek?)null;
        var shouldSyncRecurrenceWeek = previousWeekOfMonth is null || form.RecursOnWeekOfMonth == previousWeekOfMonth.Value;

        if (shouldSyncRecurrenceDay)
        {
            form.RecursOnDayOfWeek = selectedDate.DayOfWeek;
        }

        if (shouldSyncRecurrenceWeek)
        {
            form.RecursOnWeekOfMonth = GetDefaultWeekOfMonth(selectedDate);
        }
    }

    private void HandleRecursUntilChanged(ChangeEventArgs args) =>
        form.RecursUntilText = args.Value?.ToString();

    private bool IsEditing(Guid eventId) => form.EventId == eventId;

    private static string? GetImagePreviewPath(string? imagePath) =>
        MenuImagePathDisplay.Normalize(imagePath);

    private string GetScheduleSummary(EventRecord record) =>
        EventScheduleRules.GetScheduleSummary(record, TimeProvider.GetLocalNow().DateTime);

    private static string GetScheduleTypeLabel(EventRecord record) =>
        record.IsRecurring ? "Recurring" : "One Time";

    private static string GetPublicationLabel(EventPublicationState publicationState) =>
        publicationState switch
        {
            EventPublicationState.Draft => "Draft",
            EventPublicationState.Published => "Published",
            EventPublicationState.Archived => "Archived",
            _ => publicationState.ToString()
        };

    private static string GetRecurrencePatternLabel(EventRecurrencePattern recurrencePattern) =>
        recurrencePattern switch
        {
            EventRecurrencePattern.None => "One-time event",
            EventRecurrencePattern.Weekly => "Weekly",
            EventRecurrencePattern.MonthlyNthWeekday => "Monthly nth weekday",
            _ => recurrencePattern.ToString()
        };

    private static string GetRecurrenceWeekLabel(EventRecurrenceWeek recurrenceWeek) =>
        recurrenceWeek switch
        {
            EventRecurrenceWeek.First => "First",
            EventRecurrenceWeek.Second => "Second",
            EventRecurrenceWeek.Third => "Third",
            EventRecurrenceWeek.Fourth => "Fourth",
            EventRecurrenceWeek.Last => "Last",
            _ => recurrenceWeek.ToString()
        };

    private static string GetSaveStatusMessage(EventPublicationState publicationState, bool wasNewRecord) =>
        publicationState switch
        {
            EventPublicationState.Draft when wasNewRecord => "Draft event created.",
            EventPublicationState.Draft => "Draft event saved.",
            EventPublicationState.Published when wasNewRecord => "Event created and published.",
            EventPublicationState.Published => "Event saved and published.",
            EventPublicationState.Archived => "Event archived.",
            _ => "Event saved."
        };

    private static EventRecurrenceWeek GetDefaultWeekOfMonth(DateOnly date) =>
        date.Day switch
        {
            <= 7 => EventRecurrenceWeek.First,
            <= 14 => EventRecurrenceWeek.Second,
            <= 21 => EventRecurrenceWeek.Third,
            <= 28 => EventRecurrenceWeek.Fourth,
            _ => EventRecurrenceWeek.Last
        };

    private bool TryParseRequiredDate(string? value, string fieldName, out DateOnly? date)
    {
        date = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            validationErrors.Add($"{fieldName} is required.");
            return false;
        }

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            validationErrors.Add($"{fieldName} must use a valid date.");
            return false;
        }

        date = parsedDate;
        return true;
    }

    private bool TryParseOptionalDate(string? value, string fieldName, out DateOnly? date)
    {
        date = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            validationErrors.Add($"{fieldName} must use a valid date.");
            return false;
        }

        date = parsedDate;
        return true;
    }

    private bool TryParseRequiredTime(string? value, string fieldName, out TimeOnly? time)
    {
        time = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            validationErrors.Add($"{fieldName} is required.");
            return false;
        }

        if (!FlexibleTimeText.TryParse(value, out var parsedTime))
        {
            validationErrors.Add($"{fieldName} must use a valid time.");
            return false;
        }

        time = parsedTime;
        return true;
    }

    private bool TryParseOptionalTime(string? value, string fieldName, out TimeOnly? time)
    {
        time = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!FlexibleTimeText.TryParse(value, out var parsedTime))
        {
            validationErrors.Add($"{fieldName} must use a valid time.");
            return false;
        }

        time = parsedTime;
        return true;
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", InvariantCulture);

    private static string? FormatDate(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd", InvariantCulture);

    private static bool TryParseDateText(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", InvariantCulture, DateTimeStyles.None, out date);

    private static string FormatTime(TimeOnly time) =>
        FlexibleTimeText.FormatDisplay(time);

    private static string? FormatTime(TimeOnly? time) =>
        time is null ? null : FlexibleTimeText.FormatDisplay(time.Value);

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class EventEditorFormModel
    {
        public Guid? EventId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? PromoBadge { get; set; }

        public string? ImagePath { get; set; }

        public string StartsOnText { get; set; } = string.Empty;

        public string? StartsAtText { get; set; }

        public string? EndsAtText { get; set; }

        public bool EndsNextDay { get; set; }

        public int SortOrder { get; set; } = 1;

        public EventPublicationState PublicationState { get; set; } = EventPublicationState.Draft;

        public EventRecurrencePattern RecurrencePattern { get; set; }

        public int RecurrenceInterval { get; set; } = 1;

        public DayOfWeek RecursOnDayOfWeek { get; set; } = DayOfWeek.Friday;

        public EventRecurrenceWeek RecursOnWeekOfMonth { get; set; } = EventRecurrenceWeek.First;

        public string? RecursUntilText { get; set; }

        public string? TimingNotes { get; set; }
    }
}
