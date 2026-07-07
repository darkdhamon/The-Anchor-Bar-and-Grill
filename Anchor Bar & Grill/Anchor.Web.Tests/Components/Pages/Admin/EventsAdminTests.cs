using System.Security.Claims;
using Anchor.Domain.Events;
using Anchor.Domain.Identity;
using Anchor.Web.Components.Pages.Admin;
using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Anchor.Web.Tests.Components.Pages.Admin;

public sealed class EventsAdminTests : BunitContext
{
    private readonly TestAuthenticationStateProvider authStateProvider;
    private readonly FakeEventManagementService eventManagementService;

    public EventsAdminTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(ApplicationPolicies.EventManagement, policy => policy.RequireRole(ApplicationRoles.EventManager));
        });
        Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
        authStateProvider = new TestAuthenticationStateProvider();
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.FromHours(-5))));

        eventManagementService = new FakeEventManagementService();
        Services.AddSingleton<IEventManagementService>(eventManagementService);

        authStateProvider.SetUser(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "events@anchor.test"),
            new Claim(ClaimTypes.Role, ApplicationRoles.EventManager)
        ], "TestAuth")));
    }

    [Fact]
    public void EventsAdmin_SaveDraft_creates_a_draft_event_and_refreshes_the_list()
    {
        var cut = Render<EventsAdmin>();
        WaitForEventsToLoad(cut);

        cut.Find("#event-title").Input("Dock Party");
        cut.Find("#event-summary").Input("A short preview for the public event card.");
        cut.Find("#event-description").Input("A fuller description for guests who open the event listing.");
        cut.Find("#event-promo-badge").Input("Community Night");
        cut.Find("#save-draft-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(eventManagementService.LastSavedRequest);
            Assert.Equal(EventPublicationState.Draft, eventManagementService.LastSavedRequest!.PublicationState);
            Assert.Contains("Draft event created.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Dock Party", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Draft", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void EventsAdmin_Publish_and_archive_actions_update_publication_state()
    {
        var existingEvent = CreateEventRecord(
            Guid.Parse("1A794B4C-8E84-4C52-BDB9-9C7B35B6B001"),
            "Friday Live Music",
            EventPublicationState.Draft);
        eventManagementService.Events.Add(existingEvent);

        var cut = Render<EventsAdmin>();
        WaitForEventsToLoad(cut);

        cut.Find($"#edit-event-{existingEvent.EventId}").Click();
        cut.Find("#publish-event-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(eventManagementService.LastSavedRequest);
            Assert.Equal(EventPublicationState.Published, eventManagementService.LastSavedRequest!.PublicationState);
            Assert.Contains("Event saved and published.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Published", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        cut.Find("#archive-event-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(eventManagementService.LastSavedRequest);
            Assert.Equal(EventPublicationState.Archived, eventManagementService.LastSavedRequest!.PublicationState);
            Assert.Contains("Event archived.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Archived", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void EventsAdmin_Shows_validation_errors_from_save_requests()
    {
        var cut = Render<EventsAdmin>();
        WaitForEventsToLoad(cut);

        cut.Find("#event-title").Input(string.Empty);
        cut.Find("#event-summary").Input(string.Empty);
        cut.Find("#event-description").Input(string.Empty);
        cut.Find("#save-event-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Event title is required.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Event summary is required.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Event description is required.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Error: The event could not be saved.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void EventsAdmin_Delete_requires_confirmation_and_removes_the_event()
    {
        var existingEvent = CreateEventRecord(
            Guid.Parse("7ACF6D7E-A790-490D-A567-630D72FC1A11"),
            "Third Friday Steak Night",
            EventPublicationState.Published);
        eventManagementService.Events.Add(existingEvent);

        var cut = Render<EventsAdmin>();
        WaitForEventsToLoad(cut);

        cut.Find($"#delete-event-{existingEvent.EventId}").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll($"#confirm-delete-event-{existingEvent.EventId}"));
        });

        cut.Find($"#confirm-delete-event-{existingEvent.EventId}").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(existingEvent.EventId, eventManagementService.LastDeletedEventId);
            Assert.DoesNotContain("Third Friday Steak Night", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Event deleted.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void EventsAdmin_New_event_date_updates_default_monthly_recurrence_values()
    {
        var cut = Render<EventsAdmin>();
        WaitForEventsToLoad(cut);

        cut.Find("#event-title").Input("Friday Patio Party");
        cut.Find("#event-summary").Input("Recurring patio preview");
        cut.Find("#event-description").Input("Monthly recurring event details.");
        cut.Find("#event-start-date").Change("2026-07-31");
        cut.Find("#event-recurrence-pattern").Change("MonthlyNthWeekday");
        cut.Find("#save-event-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(eventManagementService.LastSavedRequest);
            Assert.Equal(EventRecurrencePattern.MonthlyNthWeekday, eventManagementService.LastSavedRequest!.RecurrencePattern);
            Assert.Equal(DayOfWeek.Friday, eventManagementService.LastSavedRequest.RecursOnDayOfWeek);
            Assert.Equal(EventRecurrenceWeek.Last, eventManagementService.LastSavedRequest.RecursOnWeekOfMonth);
        });
    }

    [Fact]
    public void EventsAdmin_Changing_date_preserves_manual_recurrence_overrides()
    {
        var cut = Render<EventsAdmin>();
        WaitForEventsToLoad(cut);

        cut.Find("#event-title").Input("Manual cadence");
        cut.Find("#event-summary").Input("Summary");
        cut.Find("#event-description").Input("Description");
        cut.Find("#event-recurrence-pattern").Change("MonthlyNthWeekday");
        cut.Find("#event-recurs-day").Change("Wednesday");
        cut.Find("#event-recurrence-week").Change("Second");
        cut.Find("#event-start-date").Change("2026-07-31");
        cut.Find("#save-event-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(eventManagementService.LastSavedRequest);
            Assert.Equal(DayOfWeek.Wednesday, eventManagementService.LastSavedRequest!.RecursOnDayOfWeek);
            Assert.Equal(EventRecurrenceWeek.Second, eventManagementService.LastSavedRequest.RecursOnWeekOfMonth);
        });
    }

    [Fact]
    public void EventsAdmin_Normalizes_relative_image_paths_for_admin_previews()
    {
        var existingEvent = CreateEventRecord(
            Guid.Parse("88760FAE-208F-416B-BD48-12F77DF8A4B5"),
            "Live Music Preview",
            EventPublicationState.Published,
            imagePath: "images/events/live-music.svg");
        eventManagementService.Events.Add(existingEvent);

        var cut = Render<EventsAdmin>();
        WaitForEventsToLoad(cut);

        var image = cut.Find("img.admin-event-thumb");

        Assert.Equal("/images/events/live-music.svg", image.GetAttribute("src"));
    }

    [Fact]
    public void EventsAdmin_Disables_save_actions_and_shows_saving_state_while_persisting()
    {
        eventManagementService.HoldNextSave();

        var cut = Render<EventsAdmin>();
        WaitForEventsToLoad(cut);

        cut.Find("#event-title").Input("Slow save event");
        cut.Find("#event-summary").Input("Summary");
        cut.Find("#event-description").Input("Description");

        cut.Find("#save-event-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, eventManagementService.SaveCallCount);
            Assert.True(cut.Find("#save-event-button").HasAttribute("disabled"));
            Assert.True(cut.Find("#save-draft-button").HasAttribute("disabled"));
            Assert.True(cut.Find("#publish-event-button").HasAttribute("disabled"));
            Assert.Contains("Saving", cut.Find("#save-event-button").TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(cut.FindAll("#save-event-button .action-button__spinner"));
        });

        cut.Find("#save-draft-button").TriggerEvent("onclick", new MouseEventArgs());
        Assert.Equal(1, eventManagementService.SaveCallCount);

        eventManagementService.ReleaseHeldSave();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, eventManagementService.SaveCallCount);
            Assert.Null(cut.Find("#save-event-button").GetAttribute("disabled"));
            Assert.Contains("Draft event created.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void EventsAdmin_Requires_event_manager_role_when_routed()
    {
        authStateProvider.SetUser(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "admin@anchor.test"),
            new Claim(ClaimTypes.Role, ApplicationRoles.Admin)
        ], "TestAuth")));

        var routeData = new RouteData(typeof(EventsAdmin), new Dictionary<string, object?>());
        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<AuthorizeRouteView>(0);
                childBuilder.AddAttribute(1, "RouteData", routeData);
                childBuilder.AddAttribute(2, "NotAuthorized", (RenderFragment<AuthenticationState>)(_ => notAuthorizedBuilder =>
                {
                    notAuthorizedBuilder.AddMarkupContent(0, "<p>Not authorized.</p>");
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Not authorized.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Event editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static void WaitForEventsToLoad(IRenderedComponent<EventsAdmin> cut)
    {
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Loading event data", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static EventRecord CreateEventRecord(Guid eventId, string title, EventPublicationState publicationState, string? imagePath = null) =>
        new(
            eventId,
            title,
            "Short summary",
            "Full guest-facing description.",
            "Live Music",
            imagePath,
            new DateOnly(2026, 7, 18),
            new TimeOnly(19, 0),
            new TimeOnly(22, 0),
            false,
            1,
            publicationState,
            EventRecurrencePattern.None,
            1,
            null,
            null,
            null,
            null);

    private sealed class FakeEventManagementService : IEventManagementService
    {
        private TaskCompletionSource<bool>? heldSaveCompletionSource;

        public List<EventRecord> Events { get; } = [];

        public SaveEventRequest? LastSavedRequest { get; private set; }

        public Guid? LastDeletedEventId { get; private set; }

        public int SaveCallCount { get; private set; }

        public void HoldNextSave() =>
            heldSaveCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseHeldSave()
        {
            heldSaveCompletionSource?.TrySetResult(true);
            heldSaveCompletionSource = null;
        }

        public Task<IReadOnlyList<EventRecord>> GetEventsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventRecord>>(
                Events
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.StartsOn)
                    .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                    .ToArray());

        public async Task<EventOperationResult> SaveEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            LastSavedRequest = request;

            if (heldSaveCompletionSource is { } pendingSave)
            {
                await pendingSave.Task;
            }

            var validationErrors = EventScheduleRules.Validate(request);
            if (validationErrors.Count > 0)
            {
                return EventOperationResult.Failure(validationErrors);
            }

            var eventId = request.EventId ?? Guid.NewGuid();
            var updatedRecord = new EventRecord(
                eventId,
                request.Title.Trim(),
                request.Summary.Trim(),
                request.Description.Trim(),
                string.IsNullOrWhiteSpace(request.PromoBadge) ? null : request.PromoBadge.Trim(),
                string.IsNullOrWhiteSpace(request.ImagePath) ? null : request.ImagePath.Trim(),
                request.StartsOn,
                request.StartsAt,
                request.EndsAt,
                request.EndsNextDay,
                request.SortOrder,
                request.PublicationState,
                request.RecurrencePattern,
                request.RecurrencePattern == EventRecurrencePattern.None ? 1 : request.RecurrenceInterval,
                request.RecurrencePattern == EventRecurrencePattern.None ? null : request.RecursOnDayOfWeek,
                request.RecurrencePattern == EventRecurrencePattern.MonthlyNthWeekday ? request.RecursOnWeekOfMonth : null,
                request.RecurrencePattern == EventRecurrencePattern.None ? null : request.RecursUntil,
                string.IsNullOrWhiteSpace(request.TimingNotes) ? null : request.TimingNotes.Trim());

            var index = Events.FindIndex(item => item.EventId == eventId);
            if (index >= 0)
            {
                Events[index] = updatedRecord;
            }
            else
            {
                Events.Add(updatedRecord);
            }

            return EventOperationResult.Success(eventId);
        }

        public Task<EventOperationResult> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            var removed = Events.RemoveAll(item => item.EventId == eventId) > 0;
            if (!removed)
            {
                return Task.FromResult(EventOperationResult.Failure("The requested event was not found."));
            }

            LastDeletedEventId = eventId;
            return Task.FromResult(EventOperationResult.Success(eventId));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset localNow) : TimeProvider
    {
        private readonly TimeZoneInfo localTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Test/Local",
            localNow.Offset,
            "Test/Local",
            "Test/Local");

        public override TimeZoneInfo LocalTimeZone => localTimeZone;

        public override DateTimeOffset GetUtcNow() => localNow.ToUniversalTime();
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private AuthenticationState authenticationState = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(authenticationState);

        public void SetUser(ClaimsPrincipal user)
        {
            authenticationState = new AuthenticationState(user);
            NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
        }
    }

    private sealed class TestAuthorizationService(IAuthorizationPolicyProvider policyProvider) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(Evaluate(user, requirements));

        public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
        {
            var policy = await policyProvider.GetPolicyAsync(policyName);
            return policy is null ? AuthorizationResult.Failed() : Evaluate(user, policy.Requirements);
        }

        private static AuthorizationResult Evaluate(ClaimsPrincipal user, IEnumerable<IAuthorizationRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                switch (requirement)
                {
                    case DenyAnonymousAuthorizationRequirement when user.Identity?.IsAuthenticated != true:
                        return AuthorizationResult.Failed();
                    case RolesAuthorizationRequirement rolesRequirement when !rolesRequirement.AllowedRoles.Any(user.IsInRole):
                        return AuthorizationResult.Failed();
                }
            }

            return AuthorizationResult.Success();
        }
    }
}
