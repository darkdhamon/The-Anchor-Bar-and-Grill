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
            "accent-blue",
            "Served with the menu's bright, approachable bar-and-grill energy.",
            [
                new("Cheese Curds", "Crisp white cheddar curds with your choice of dipping sauce.", "$9", new("images/menu/appetizers.svg", "Mockup food photo for cheese curds")),
                new("Mini Tacos", "Served with salsa and sour cream.", "$9", Offer: OfferStartingIn(14)),
                new("Quesadillas", "Loaded with cheese and served with salsa and sour cream.", "$11", Offer: OfferStartingIn(-8, 62), IsSeasonal: true),
                new("Fish Tacos", "Finished with Boom Boom sauce for a bold bar-food favorite.", "$10", new("images/menu/appetizers.svg", "Mockup food photo for fish tacos"), OfferStartingIn(-5, 22))
            ]),
        new(
            "Wings",
            "accent-blue",
            "Mockup sauces mirror the flavor-forward tone of the printed menu.",
            [
                new("Traditional or Boneless (6)", "Choice of one sauce.", "$9", new("images/menu/wings.svg", "Mockup food photo for a basket of wings")),
                new("Traditional or Boneless (12)", "Choice of two sauces.", "$16", new("images/menu/wings.svg", "Mockup food photo for a platter of wings")),
                new("Add Fries", "Upgrade any wing order with a side of fries.", "$3")
            ]),
        new(
            "Soups & Salads",
            "accent-green",
            "A lighter section that still feels part of the same menu family.",
            [
                new("The Anchor Salad", "Crisp greens, tomatoes, peppers, shaved red onions, and your choice of dressing.", "$10", new("images/menu/salads.svg", "Mockup food photo for the Anchor salad")),
                new("Smoked Salmon Salad", "Finished with crumbled smoked salmon and poppyseed dressing.", "$13", new("images/menu/salads.svg", "Mockup food photo for a smoked salmon salad"), OfferStartingIn(9, 45), true),
                new("BLT Salad", "Bacon, tomatoes, greens, and your choice of dressing.", "$12"),
                new("Seasonal Soup", "Cup or bowl, updated as the kitchen rotates specials.", "$4/$6")
            ]),
        new(
            "Sandwiches",
            "accent-gold",
            "Hearty handhelds inspired by the current printed menu.",
            [
                new("Grilled Chicken Sandwich", "Lettuce, tomato, and mayo on a toasted bun.", "$13", new("images/menu/sandwiches.svg", "Mockup food photo for a grilled chicken sandwich")),
                new("Steak Sandwich", "Grilled sirloin, smoked gouda, peppers, and onions.", "$14", new("images/menu/sandwiches.svg", "Mockup food photo for a steak sandwich")),
                new("Ranch Melt", "Swiss cheese, grilled ham, smoked bacon, and classic ranch.", "$13"),
                new("Walleye Sandwich", "Breaded walleye on a toasted bun.", "$14")
            ]),
        new(
            "Burgers",
            "accent-magenta",
            "Big labels and clean pricing should make the burger section easy to scan.",
            [
                new("Classic Hamburger", "Fresh hand-pattied burger; add cheese if desired.", "$11", new("images/menu/burgers.svg", "Mockup food photo for a classic hamburger")),
                new("Bacon Cheeseburger", "A familiar favorite with bacon and melty cheese.", "$13", new("images/menu/burgers.svg", "Mockup food photo for a bacon cheeseburger")),
                new("Western Burger", "Bacon, BBQ sauce, and a crisp onion ring.", "$14"),
                new("Sunrise Burger", "Bacon, American cheese, and egg.", "$14")
            ]),
        new(
            "Wraps",
            "accent-blue",
            "Bright blue framed panels from the menu carry over nicely here.",
            [
                new("Chicken Wrap", "Grilled or crispy chicken, tomatoes, onions, lettuce, and dressing.", "$13", new("images/menu/wraps.svg", "Mockup food photo for a chicken wrap")),
                new("Steak Wrap", "Steak, peppers, onions, cheese, lettuce, and dressing.", "$13", new("images/menu/wraps.svg", "Mockup food photo for a steak wrap")),
                new("Buffalo Chicken Wrap", "Crispy chicken tossed in buffalo with ranch-style cooling balance.", "$13")
            ]),
        new(
            "Kids Menu",
            "accent-magenta",
            "A small playful section with clearer sizing and pricing for families.",
            [
                new("Mac & Cheese", "Served with one side and a kid drink mockup option.", "$7"),
                new("Mini Corn Dogs", "The classic choice for a quick family meal.", "$7", new("images/menu/kids.svg", "Mockup food photo for mini corn dogs")),
                new("Chicken Strips", "Served with sauce.", "$7")
            ]),
        new(
            "Desserts",
            "accent-magenta",
            "Accent color keeps the end of the menu feeling celebratory.",
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

    public static IReadOnlyList<ScheduledEvent> FeaturedEvents { get; } =
    [
        new("Thursday Trivia", "Every Thursday", "7:00 PM", "Team-based trivia with rotating categories and a featured appetizer special.", "Weekly Favorite"),
        new("Friday Live Music", "Fridays", "8:30 PM", "A rotating lineup of regional acts with a brighter evening atmosphere.", "Live Music"),
        new("Saturday Patio Social", "Saturdays", "4:00 PM", "A relaxed pre-dinner hangout with drinks, snacks, and a family-friendly tone.", "Seasonal"),
        new("Sunday Watch Party", "Sundays", "Game Time", "Large-screen viewing, baskets, burgers, and a crowd-friendly setup.", "Game Day")
    ];

    public static IReadOnlyList<WeeklyRhythm> WeeklyRhythms { get; } =
    [
        new("Weeknights", "Give regulars a dependable rhythm with trivia, music, or simple dinner specials."),
        new("Weekends", "Lean into the Anchor as both a casual stop and a local social destination."),
        new("Private Events", "Reserve room in the mockup for future bookings, community nights, or team gatherings.")
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

    public static IReadOnlyList<string> BuildingPhotoNotes { get; } =
    [
        "This first mockup uses a styled exterior-photo placeholder because a building image has not been added to the repo yet.",
        "Once a real building photo is available, the homepage hero should swap the placeholder for the final image treatment without changing the surrounding layout."
    ];

    private static OfferWindow OfferStartingIn(int startOffsetDays, int? durationDays = null)
    {
        var startsOn = OfferReferenceDate.AddDays(startOffsetDays);
        DateOnly? endsOn = durationDays is null ? null : startsOn.AddDays(durationDays.Value);

        return new(startsOn, endsOn);
    }
}

public sealed record ContactDetails(string StreetAddress, string CityState, string PhoneNumber, string EmailNote);

public sealed record HighlightCard(string Title, string Description, string Eyebrow);

public sealed record HeroAction(string Href, string Label, bool IsPrimary);

public sealed record FeatureCallout(string Title, string Description);

public sealed record MenuSection(string Title, string AccentClass, string Note, IReadOnlyList<MenuItem> Items);

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
            ? $"{MenuPlacement} · Featured item: {RelatedMenuItem}"
            : MenuPlacement;
}

public sealed record ScheduledEvent(string Title, string DayLabel, string TimeLabel, string Description, string Badge);

public sealed record WeeklyRhythm(string Label, string Description);

public sealed record StoryPoint(string Description);

public sealed record ExperienceCard(string Title, string Description);

public sealed record HourBlock(string Label, string Hours);

public sealed record ContactChannel(string Title, string Value, string Description);
