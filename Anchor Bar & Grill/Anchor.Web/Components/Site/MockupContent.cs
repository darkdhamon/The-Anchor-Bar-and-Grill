using System.Globalization;

namespace Anchor.Web.Components.Site;

public static class MockupContent
{
    private static readonly DateOnly OfferReferenceDate = DateOnly.FromDateTime(DateTime.Today);

    public static ContactDetails Contact { get; } = new(
        "301 Main Street",
        "Madison Lake, MN",
        "(507) 243-4338",
        "Add guest-facing email before launch");

    public static IReadOnlyList<HighlightCard> HomeHighlights { get; } =
    [
        new("Lake-town comfort food", "Anchored in familiar favorites, baskets, burgers, salads, and shareable starters.", "Guest Favorite"),
        new("Events worth staying for", "Live music, trivia nights, and game-day energy should always be one click away.", "Weekly Rhythm"),
        new("Easy for first-time visitors", "The homepage should act like a welcome host and steer guests toward menu, events, and contact details.", "Mockup Goal")
    ];

    public static IReadOnlyList<HeroAction> HomeActions { get; } =
    [
        new("/menu", "Browse the Menu", true),
        new("/events", "See Upcoming Events", false)
    ];

    public static IReadOnlyList<FeatureCallout> HomeCallouts { get; } =
    [
        new("For Guests", "Start with the menu, upcoming events, and the information you need before you visit."),
        new("For Staff", "Admin mockups in this branch preview how menu, event, about, and contact content can be managed later.")
    ];

    public static IReadOnlyList<MenuSection> MenuSections { get; } =
    [
        new(
            "Appetizers",
            "Navy menu placards, cream rules, and weathered wood texture keep this section tied to the July menu art.",
            [
                new("Cheese Curds", "Crisp white cheddar curds with your choice of dipping sauce.", "$9", new("images/menu/appetizers.svg", "Mockup food photo for cheese curds")),
                new("Mini Tacos", "Served with salsa and sour cream.", "$9", Offer: OfferStartingIn(14)),
                new("Quesadillas", "Loaded with cheese and served with salsa and sour cream.", "$11", Offer: OfferStartingIn(-8, 62), IsSeasonal: true),
                new("Fish Tacos", "Finished with Boom Boom sauce for a bold bar-food favorite.", "$10", new("images/menu/appetizers.svg", "Mockup food photo for fish tacos"), OfferStartingIn(-5, 22))
            ]),
        new(
            "Wings",
            "Flavor-forward copy should stay easy to scan inside the same printed-menu frame.",
            [
                new("Traditional or Boneless (6)", "Choice of one sauce.", "$9", new("images/menu/wings.svg", "Mockup food photo for a basket of wings")),
                new("Traditional or Boneless (12)", "Choice of two sauces.", "$16", new("images/menu/wings.svg", "Mockup food photo for a platter of wings")),
                new("Add Fries", "Upgrade any wing order with a side of fries.", "$3")
            ]),
        new(
            "Soups & Salads",
            "A lighter section that still feels part of the same menu family.",
            [
                new("The Anchor Salad", "Crisp greens, tomatoes, peppers, shaved red onions, and your choice of dressing.", "$10", new("images/menu/salads.svg", "Mockup food photo for the Anchor salad")),
                new("Smoked Salmon Salad", "Finished with crumbled smoked salmon and poppyseed dressing.", "$13", new("images/menu/salads.svg", "Mockup food photo for a smoked salmon salad"), OfferStartingIn(9, 45), true),
                new("BLT Salad", "Bacon, tomatoes, greens, and your choice of dressing.", "$12"),
                new("Seasonal Soup", "Cup or bowl, updated as the kitchen rotates specials.", "$4/$6")
            ]),
        new(
            "Sandwiches",
            "Hearty handhelds inspired by the current printed menu.",
            [
                new("Grilled Chicken Sandwich", "Lettuce, tomato, and mayo on a toasted bun.", "$13", new("images/menu/sandwiches.svg", "Mockup food photo for a grilled chicken sandwich")),
                new("Steak Sandwich", "Grilled sirloin, smoked gouda, peppers, and onions.", "$14", new("images/menu/sandwiches.svg", "Mockup food photo for a steak sandwich")),
                new("Ranch Melt", "Swiss cheese, grilled ham, smoked bacon, and classic ranch.", "$13"),
                new("Walleye Sandwich", "Breaded walleye on a toasted bun.", "$14")
            ]),
        new(
            "Burgers",
            "Big labels and clean pricing should make the burger section easy to scan.",
            [
                new("Classic Hamburger", "Fresh hand-pattied burger; add cheese if desired.", "$11", new("images/menu/burgers.svg", "Mockup food photo for a classic hamburger")),
                new("Bacon Cheeseburger", "A familiar favorite with bacon and melty cheese.", "$13", new("images/menu/burgers.svg", "Mockup food photo for a bacon cheeseburger")),
                new("Western Burger", "Bacon, BBQ sauce, and a crisp onion ring.", "$14"),
                new("Sunrise Burger", "Bacon, American cheese, and egg.", "$14")
            ]),
        new(
            "Wraps",
            "Consistent navy headers and cream dividers keep wraps aligned with the new menu instead of a separate color theme.",
            [
                new("Chicken Wrap", "Grilled or crispy chicken, tomatoes, onions, lettuce, and dressing.", "$13", new("images/menu/wraps.svg", "Mockup food photo for a chicken wrap")),
                new("Steak Wrap", "Steak, peppers, onions, cheese, lettuce, and dressing.", "$13", new("images/menu/wraps.svg", "Mockup food photo for a steak wrap")),
                new("Buffalo Chicken Wrap", "Crispy chicken tossed in buffalo with ranch-style cooling balance.", "$13")
            ]),
        new(
            "Kids Menu",
            "A small playful section with clearer sizing and pricing for families.",
            [
                new("Mac & Cheese", "Served with one side and a kid drink mockup option.", "$7"),
                new("Mini Corn Dogs", "The classic choice for a quick family meal.", "$7", new("images/menu/kids.svg", "Mockup food photo for mini corn dogs")),
                new("Chicken Strips", "Served with sauce.", "$7")
            ]),
        new(
            "Desserts",
            "The final section can feel celebratory through type and spacing without changing the header color.",
            [
                new("Chocolate Lava Cake", "Served warm with ice cream.", "$6", new("images/menu/desserts.svg", "Mockup food photo for chocolate lava cake"), OfferStartingIn(-2, 18)),
                new("Mini Donuts", "Fair-style donuts for a casual sweet finish.", "$6", new("images/menu/desserts.svg", "Mockup food photo for mini donuts"))
            ])
    ];

    public static IReadOnlyList<RecurringSpecial> RecurringSpecials { get; } =
    [
        new(DayOfWeek.Monday, "Monday Night Burgers", "A dependable burger-night draw with fries and easy weeknight pricing.", "$11 basket special", "After 5:00 PM", "Weekly specials block", "Classic Hamburger"),
        new(DayOfWeek.Tuesday, "Tuesday Taco Basket", "A taco-night feature built for quick dinner traffic and casual bar seating.", "$10 dinner feature", "After 4:00 PM", "Weekly specials block", "Fish Tacos"),
        new(DayOfWeek.Wednesday, "Wing Night", "Sauced wings with a strong shareable hook for midweek regulars.", "$16 dozen special", "After 5:00 PM", "Weekly specials block", "Traditional or Boneless (12)"),
        new(DayOfWeek.Friday, "Friday Fish Fry", "A Friday dinner anchor that deserves a permanent home in the guest menu flow.", "$15 dinner plate", "After 4:00 PM", "Weekly specials block", "Walleye Sandwich"),
        new(DayOfWeek.Sunday, "Sunday Pork Chop Dinner", "A hearty end-of-week dinner special that should read as a repeatable tradition.", "$17 dinner plate", "After 3:00 PM", "Weekly specials block")
    ];

    public static IReadOnlyList<EventDefinition> GetEventDefinitions(DateOnly referenceDate) =>
    [
        new(
            "Thursday Trivia",
            "Team-based trivia with rotating categories and an appetizer special.",
            "Weekly Favorite",
            new(19, 0),
            new("images/events/trivia-night.svg", "Mockup event image for Thursday trivia"),
            StartsOn: NextOccurrenceOnOrAfter(referenceDate.AddDays(-14), DayOfWeek.Thursday),
            Recurrence: new(
                EventRecurrencePattern.Weekly,
                DayOfWeek.Thursday,
                EndsOn: NextOccurrenceOnOrAfter(referenceDate.AddDays(35), DayOfWeek.Thursday))),
        new(
            "Friday Live Music",
            "A rotating lineup of regional acts with a brighter evening atmosphere.",
            "Live Music",
            new(20, 30),
            new("images/events/live-music.svg", "Mockup event image for Friday live music"),
            StartsOn: NextOccurrenceOnOrAfter(referenceDate.AddDays(-7), DayOfWeek.Friday),
            Recurrence: new(
                EventRecurrencePattern.Weekly,
                DayOfWeek.Friday,
                Interval: 2,
                EndsOn: NextOccurrenceOnOrAfter(referenceDate.AddDays(49), DayOfWeek.Friday))),
        new(
            "Third Friday Steak Night",
            "A once-a-month dinner event that should read clearly as the third Friday tradition.",
            "Monthly Feature",
            new(18, 30),
            StartsOn: GetNthWeekdayOfMonth(referenceDate.Year, referenceDate.Month, DayOfWeek.Friday, EventRecurrenceWeek.Third),
            Recurrence: new(
                EventRecurrencePattern.MonthlyNthWeekday,
                DayOfWeek.Friday,
                WeekOfMonth: EventRecurrenceWeek.Third,
                EndsOn: GetNthWeekdayOfMonth(referenceDate.AddMonths(2).Year, referenceDate.AddMonths(2).Month, DayOfWeek.Friday, EventRecurrenceWeek.Third))),
        new(
            "Summer Kickoff Patio Party",
            "Kick off patio season with shared plates, a feature drink menu, and extended evening energy.",
            "Seasonal",
            new(18, 0),
            new("images/events/patio-party.svg", "Mockup event image for a patio party"),
            StartsOn: referenceDate.AddDays(12)),
        new(
            "Community Bingo Fundraiser",
            "A family-friendly fundraiser event with raffle prizes and simple dinner specials.",
            "Community Night",
            new(11, 0),
            StartsOn: referenceDate.AddDays(25))
    ];

    public static IReadOnlyList<string> GetEventBadgeOptions(DateOnly referenceDate) =>
        GetEventDefinitions(referenceDate)
            .Select(item => item.PromoBadge)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IReadOnlyList<WeeklyRhythm> WeeklyRhythms { get; } =
    [
        new("Weeknights", "Give regulars a dependable rhythm with trivia, music, or simple dinner specials."),
        new("Every Other Week", "Some live entertainment or community programs may intentionally land every other week instead of every week."),
        new("Monthly Traditions", "Reserve space for events like third-Friday dinners or monthly featured nights that guests learn to expect.")
    ];

    public static IReadOnlyList<StoryPoint> AboutStory { get; } =
    [
        new("The Anchor should feel like a local gathering place first, with the website helping guests understand the atmosphere before they ever walk in."),
        new("The mockup should balance approachable family dining, bar-and-grill comfort food, and event-driven energy without feeling too formal."),
        new("The visual system should borrow from the printed menu so the website feels like the same business, not a disconnected rebrand.")
    ];

    public static IReadOnlyList<ExperienceCard> ExperiencePillars { get; } =
    [
        new("Come as you are", "The tone should feel warm, direct, and welcoming for first-time visitors."),
        new("Stay for the atmosphere", "Events and hospitality should feel as central as the food itself."),
        new("Find what you need fast", "Hours, location, menu highlights, and events should never feel buried.")
    ];

    public static IReadOnlyList<HourBlock> PreviewHours { get; } =
    [
        new("Monday Burger Night", "5:00 PM - 8:00 PM"),
        new("Tuesday - Thursday", "11:00 AM - 9:00 PM"),
        new("Friday", "11:00 AM - 10:00 PM"),
        new("Saturday", "10:00 AM - 10:00 PM"),
        new("Sunday", "10:00 AM - 8:00 PM"),
        new("Special Hours Note", "Recurring specials may add day-specific dinner windows")
    ];

    public static IReadOnlyList<ContactChannel> ContactChannels { get; } =
    [
        new("Call Ahead", "(507) 243-4338", "Use for takeout questions, seating updates, or same-day information."),
        new("Visit Us", "301 Main Street, Madison Lake, MN", "The printed menu already points guests here, so the site should make directions easy too."),
        new("Website Form", "Guest inquiry mockup", "Use the mockup form to plan how questions about events, reservations, or groups will be handled.")
    ];

    public static IReadOnlyList<SocialProfile> SocialProfiles { get; } =
    [
        new("Facebook", "The Anchor Bar & Grill", "https://www.facebook.com/theanchorbarandgrill", "Use the main page for weekly specials, event reminders, and community updates."),
        new("Instagram", "@anchorbarandgrill", "https://www.instagram.com/anchorbarandgrill", "Food photos, patio moments, and short day-of-visit updates can live here."),
        new("TikTok", "@anchornights", "https://www.tiktok.com/@anchornights", "This second-style profile shows the mockup can support more than one social presence when needed.")
    ];

    public static IReadOnlyList<string> SocialPlatformOptions { get; } =
        SocialProfiles
            .Select(item => item.Platform)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IReadOnlyList<string> BuildingPhotoNotes { get; } =
    [
        "This first mockup uses a styled exterior-photo placeholder because a building image has not been added to the repo yet.",
        "Once a real building photo is available, the homepage hero should swap the placeholder for the final image treatment without changing the surrounding layout."
    ];

    public static IReadOnlyList<UpcomingEventOccurrence> GetUpcomingEvents(DateOnly fromDate) =>
        GetUpcomingEvents(fromDate, GetEventPreviewEndDate(fromDate));

    public static IReadOnlyList<UpcomingEventOccurrence> GetUpcomingEvents(DateOnly fromDate, int daysAhead) =>
        GetUpcomingEvents(fromDate, fromDate.AddDays(daysAhead));

    private static OfferWindow OfferStartingIn(int startOffsetDays, int? durationDays = null)
    {
        var startsOn = OfferReferenceDate.AddDays(startOffsetDays);
        DateOnly? endsOn = durationDays is null ? null : startsOn.AddDays(durationDays.Value);

        return new(startsOn, endsOn);
    }

    private static IReadOnlyList<UpcomingEventOccurrence> GetUpcomingEvents(DateOnly fromDate, DateOnly throughDate)
    {
        List<UpcomingEventOccurrence> upcomingEvents = [];
        var eventDefinitions = GetEventDefinitions(fromDate);

        foreach (var item in eventDefinitions)
        {
            foreach (var occursOn in item.GetOccurrences(fromDate, throughDate))
            {
                upcomingEvents.Add(new(item, occursOn));
            }
        }

        return upcomingEvents
            .OrderBy(item => item.SortAt)
            .ToArray();
    }

    private static DateOnly GetEventPreviewEndDate(DateOnly fromDate)
    {
        var lastScheduledDate = GetEventDefinitions(fromDate)
            .Select(item => item.GetPreviewEndDate())
            .DefaultIfEmpty(fromDate)
            .Max();

        return lastScheduledDate < fromDate ? fromDate : lastScheduledDate;
    }

    private static DateOnly NextOccurrenceOnOrAfter(DateOnly fromDate, DayOfWeek dayOfWeek)
    {
        var offset = ((int)dayOfWeek - (int)fromDate.DayOfWeek + 7) % 7;
        return fromDate.AddDays(offset);
    }

    private static DateOnly GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, EventRecurrenceWeek weekOfMonth)
    {
        if (weekOfMonth == EventRecurrenceWeek.Last)
        {
            var lastDayOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var offsetFromEnd = ((int)lastDayOfMonth.DayOfWeek - (int)dayOfWeek + 7) % 7;

            return lastDayOfMonth.AddDays(-offsetFromEnd);
        }

        var firstDayOfMonth = new DateOnly(year, month, 1);
        var offsetFromStart = ((int)dayOfWeek - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
        var candidate = firstDayOfMonth.AddDays(offsetFromStart + (7 * ((int)weekOfMonth - 1)));

        return candidate.Month == month ? candidate : firstDayOfMonth;
    }
}

public sealed record ContactDetails(string StreetAddress, string CityState, string PhoneNumber, string EmailNote);

public sealed record HighlightCard(string Title, string Description, string Eyebrow);

public sealed record HeroAction(string Href, string Label, bool IsPrimary);

public sealed record FeatureCallout(string Title, string Description);

public sealed record MenuSection(string Title, string Note, IReadOnlyList<MenuItem> Items);

public sealed record MenuItem(
    string Name,
    string Description,
    string Price,
    MenuItemImage? Image = null,
    OfferWindow? Offer = null,
    bool IsSeasonal = false)
{
    public bool IsSeasonalOffer => Offer?.EndsOn is not null && IsSeasonal;

    public bool IsLimitedTimeSpecial => Offer?.EndsOn is not null && !IsSeasonal;

    public bool IsComingSoon(DateOnly today) =>
        Offer is not null
        && Offer.StartsOn > today
        && Offer.StartsOn <= today.AddDays(30);

    public IReadOnlyList<string> GetStatusLabels(DateOnly today)
    {
        List<string> labels = [];

        if (IsComingSoon(today))
        {
            labels.Add("Coming Soon");
        }

        if (IsSeasonalOffer)
        {
            labels.Add("Seasonal");
        }
        else if (IsLimitedTimeSpecial)
        {
            labels.Add("Limited Time Special");
        }

        return labels;
    }

    public string? GetOfferDateSummary(DateOnly today)
    {
        if (Offer is null)
        {
            return null;
        }

        if (Offer.EndsOn is null)
        {
            return Offer.StartsOn > today
                ? $"Expected on {Offer.StartsOn:MMM d}"
                : $"Available since {Offer.StartsOn:MMM d}";
        }

        return Offer.StartsOn > today
            ? $"Offered {Offer.StartsOn:MMM d} - {Offer.EndsOn:MMM d}"
            : $"Available through {Offer.EndsOn:MMM d}";
    }
}

public sealed record MenuItemImage(string Source, string AltText);

public sealed record OfferWindow(DateOnly StartsOn, DateOnly? EndsOn);

public sealed record RecurringSpecial(
    DayOfWeek DayOfWeek,
    string Title,
    string Description,
    string PriceNote,
    string TimeLabel,
    string MenuPlacement,
    string? RelatedMenuItem = null)
{
    public string DayLabel => DayOfWeek switch
    {
        DayOfWeek.Monday => "Monday",
        DayOfWeek.Tuesday => "Tuesday",
        DayOfWeek.Wednesday => "Wednesday",
        DayOfWeek.Thursday => "Thursday",
        DayOfWeek.Friday => "Friday",
        DayOfWeek.Saturday => "Saturday",
        DayOfWeek.Sunday => "Sunday",
        _ => DayOfWeek.ToString()
    };

    public bool IsToday(DateOnly today) => today.DayOfWeek == DayOfWeek;

    public string PlacementSummary =>
        RelatedMenuItem is { Length: > 0 }
            ? $"{MenuPlacement} - Featured item: {RelatedMenuItem}"
            : MenuPlacement;
}

public sealed record EventDefinition(
    string Title,
    string Description,
    string PromoBadge,
    TimeOnly StartsAt,
    EventImage? Image = null,
    DateOnly? StartsOn = null,
    EventRecurrence? Recurrence = null)
{
    public bool IsRecurring => Recurrence is not null;

    public string TimeLabel => StartsAt.ToString("h:mm tt", CultureInfo.InvariantCulture);

    public string DateInputValue => (StartsOn ?? DateOnly.FromDateTime(DateTime.Today)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public string TimeInputValue => StartsAt.ToString("HH:mm", CultureInfo.InvariantCulture);

    public DateOnly GetPreviewEndDate() => Recurrence?.EndsOn ?? StartsOn ?? DateOnly.FromDateTime(DateTime.Today);

    public IReadOnlyList<DateOnly> GetOccurrences(DateOnly fromDate, DateOnly throughDate)
    {
        var startDate = StartsOn ?? fromDate;

        if (Recurrence is null)
        {
            return startDate >= fromDate && startDate <= throughDate ? [startDate] : [];
        }

        return Recurrence.GetOccurrences(startDate, fromDate, throughDate);
    }

    public DateOnly? GetNextOccurrence(DateOnly fromDate)
    {
        var startDate = StartsOn ?? fromDate;

        if (Recurrence is null)
        {
            return startDate >= fromDate ? startDate : null;
        }

        return Recurrence.GetNextOccurrence(startDate, fromDate);
    }

    public string GetScheduleSummary(DateOnly fromDate)
    {
        if (Recurrence is null)
        {
            return StartsOn is { } oneTimeDate
                ? $"One-time event on {oneTimeDate:MMM d, yyyy} at {TimeLabel}"
                : $"One-time event at {TimeLabel}";
        }

        return Recurrence.GetSummary(StartsOn ?? fromDate, StartsAt, fromDate);
    }
}

public sealed record EventImage(string Source, string AltText);

public sealed record EventRecurrence(
    EventRecurrencePattern Pattern,
    DayOfWeek DayOfWeek,
    int Interval = 1,
    EventRecurrenceWeek WeekOfMonth = EventRecurrenceWeek.First,
    DateOnly? EndsOn = null)
{
    public IReadOnlyList<DateOnly> GetOccurrences(DateOnly startDate, DateOnly fromDate, DateOnly throughDate)
    {
        if (throughDate < fromDate)
        {
            return [];
        }

        var finalDate = EndsOn is { } endsOn && endsOn < throughDate ? endsOn : throughDate;

        if (finalDate < fromDate)
        {
            return [];
        }

        return Pattern switch
        {
            EventRecurrencePattern.Weekly => GetWeeklyOccurrences(startDate, fromDate, finalDate),
            EventRecurrencePattern.MonthlyNthWeekday => GetMonthlyNthWeekdayOccurrences(startDate, fromDate, finalDate),
            _ => []
        };
    }

    public DateOnly? GetNextOccurrence(DateOnly startDate, DateOnly fromDate)
    {
        var searchEnd = EndsOn ?? fromDate.AddYears(1);
        var occurrences = GetOccurrences(startDate, fromDate, searchEnd);

        return occurrences.Count > 0 ? occurrences[0] : null;
    }

    public string GetSummary(DateOnly startDate, TimeOnly startsAt, DateOnly fromDate)
    {
        var timeLabel = startsAt.ToString("h:mm tt", CultureInfo.InvariantCulture);
        var cadence = Pattern switch
        {
            EventRecurrencePattern.Weekly when Interval == 1 => $"Recurring every {GetDayLabel(DayOfWeek)} at {timeLabel}",
            EventRecurrencePattern.Weekly when Interval == 2 => $"Recurring every other {GetDayLabel(DayOfWeek)} at {timeLabel}",
            EventRecurrencePattern.Weekly => $"Recurring every {Interval} weeks on {GetDayLabel(DayOfWeek)} at {timeLabel}",
            EventRecurrencePattern.MonthlyNthWeekday when Interval == 1 => $"Recurring on the {GetWeekLabel(WeekOfMonth).ToLowerInvariant()} {GetDayLabel(DayOfWeek)} of each month at {timeLabel}",
            EventRecurrencePattern.MonthlyNthWeekday => $"Recurring every {Interval} months on the {GetWeekLabel(WeekOfMonth).ToLowerInvariant()} {GetDayLabel(DayOfWeek)} at {timeLabel}",
            _ => $"Recurring at {timeLabel}"
        };

        return GetNextOccurrence(startDate, fromDate) is { } nextDate
            ? $"{cadence} - next on {nextDate:MMM d, yyyy}"
            : cadence;
    }

    public string GetPatternLabel() => Pattern switch
    {
        EventRecurrencePattern.Weekly when Interval == 1 => $"Every {GetDayLabel(DayOfWeek)}",
        EventRecurrencePattern.Weekly when Interval == 2 => $"Every other {GetDayLabel(DayOfWeek)}",
        EventRecurrencePattern.Weekly => $"Every {Interval} weeks on {GetDayLabel(DayOfWeek)}",
        EventRecurrencePattern.MonthlyNthWeekday when Interval == 1 => $"{GetWeekLabel(WeekOfMonth)} {GetDayLabel(DayOfWeek)} of the month",
        EventRecurrencePattern.MonthlyNthWeekday => $"{GetWeekLabel(WeekOfMonth)} {GetDayLabel(DayOfWeek)} every {Interval} months",
        _ => "Recurring schedule"
    };

    private IReadOnlyList<DateOnly> GetWeeklyOccurrences(DateOnly startDate, DateOnly fromDate, DateOnly throughDate)
    {
        List<DateOnly> occurrences = [];
        var anchorDate = NextOccurrenceOnOrAfter(startDate, DayOfWeek);
        var occurrenceDate = anchorDate > fromDate ? anchorDate : NextOccurrenceOnOrAfter(fromDate, DayOfWeek);

        if (Interval > 1)
        {
            var weeksFromAnchor = (occurrenceDate.DayNumber - anchorDate.DayNumber) / 7;
            var remainder = weeksFromAnchor % Interval;

            if (remainder != 0)
            {
                occurrenceDate = occurrenceDate.AddDays((Interval - remainder) * 7);
            }
        }

        while (occurrenceDate <= throughDate)
        {
            occurrences.Add(occurrenceDate);
            occurrenceDate = occurrenceDate.AddDays(Interval * 7);
        }

        return occurrences;
    }

    private IReadOnlyList<DateOnly> GetMonthlyNthWeekdayOccurrences(DateOnly startDate, DateOnly fromDate, DateOnly throughDate)
    {
        List<DateOnly> occurrences = [];
        var anchorMonth = new DateOnly(startDate.Year, startDate.Month, 1);
        var cursorDate = startDate > fromDate ? startDate : fromDate;
        var cursorMonth = new DateOnly(cursorDate.Year, cursorDate.Month, 1);
        var finalMonth = new DateOnly(throughDate.Year, throughDate.Month, 1);

        while (cursorMonth <= finalMonth)
        {
            var monthOffset = ((cursorMonth.Year - anchorMonth.Year) * 12) + cursorMonth.Month - anchorMonth.Month;

            if (monthOffset >= 0 && monthOffset % Interval == 0)
            {
                var occurrenceDate = GetNthWeekdayOfMonth(cursorMonth.Year, cursorMonth.Month, DayOfWeek, WeekOfMonth);

                if (occurrenceDate >= startDate && occurrenceDate >= fromDate && occurrenceDate <= throughDate)
                {
                    occurrences.Add(occurrenceDate);
                }
            }

            cursorMonth = cursorMonth.AddMonths(1);
        }

        return occurrences;
    }

    private static DateOnly NextOccurrenceOnOrAfter(DateOnly fromDate, DayOfWeek dayOfWeek)
    {
        var offset = ((int)dayOfWeek - (int)fromDate.DayOfWeek + 7) % 7;
        return fromDate.AddDays(offset);
    }

    private static DateOnly GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, EventRecurrenceWeek weekOfMonth)
    {
        if (weekOfMonth == EventRecurrenceWeek.Last)
        {
            var lastDayOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var offsetFromEnd = ((int)lastDayOfMonth.DayOfWeek - (int)dayOfWeek + 7) % 7;

            return lastDayOfMonth.AddDays(-offsetFromEnd);
        }

        var firstDayOfMonth = new DateOnly(year, month, 1);
        var offsetFromStart = ((int)dayOfWeek - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
        var candidate = firstDayOfMonth.AddDays(offsetFromStart + (7 * ((int)weekOfMonth - 1)));

        return candidate.Month == month ? candidate : firstDayOfMonth;
    }

    private static string GetDayLabel(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "Monday",
        DayOfWeek.Tuesday => "Tuesday",
        DayOfWeek.Wednesday => "Wednesday",
        DayOfWeek.Thursday => "Thursday",
        DayOfWeek.Friday => "Friday",
        DayOfWeek.Saturday => "Saturday",
        DayOfWeek.Sunday => "Sunday",
        _ => dayOfWeek.ToString()
    };

    private static string GetWeekLabel(EventRecurrenceWeek weekOfMonth) => weekOfMonth switch
    {
        EventRecurrenceWeek.First => "First",
        EventRecurrenceWeek.Second => "Second",
        EventRecurrenceWeek.Third => "Third",
        EventRecurrenceWeek.Fourth => "Fourth",
        EventRecurrenceWeek.Last => "Last",
        _ => "First"
    };
}

public enum EventRecurrencePattern
{
    Weekly,
    MonthlyNthWeekday
}

public enum EventRecurrenceWeek
{
    First = 1,
    Second = 2,
    Third = 3,
    Fourth = 4,
    Last = 5
}

public sealed record UpcomingEventOccurrence(EventDefinition SourceEvent, DateOnly OccursOn)
{
    public string Title => SourceEvent.Title;

    public string Description => SourceEvent.Description;

    public string PromoBadge => SourceEvent.PromoBadge;

    public EventImage? Image => SourceEvent.Image;

    public bool IsRecurring => SourceEvent.IsRecurring;

    public string ScheduleTypeLabel => IsRecurring ? "Recurring" : "One Time";

    public string DateLabel => $"{OccursOn:ddd, MMM d, yyyy}";

    public string DateTimeLabel => $"{OccursOn:ddd, MMM d, yyyy} at {SourceEvent.TimeLabel}";

    public string ScheduleDetail => SourceEvent.Recurrence?.GetPatternLabel() ?? "Scheduled one time";

    public DateTime SortAt => OccursOn.ToDateTime(SourceEvent.StartsAt);
}

public sealed record WeeklyRhythm(string Label, string Description);

public sealed record StoryPoint(string Description);

public sealed record ExperienceCard(string Title, string Description);

public sealed record HourBlock(string Label, string Hours);

public sealed record ContactChannel(string Title, string Value, string Description);

public sealed record SocialProfile(string Platform, string ProfileLabel, string Url, string Description);
