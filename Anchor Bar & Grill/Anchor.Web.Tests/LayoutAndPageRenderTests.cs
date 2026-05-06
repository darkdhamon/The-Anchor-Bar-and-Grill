using Anchor.Web.Components.Layout;
using Anchor.Web.Components.Pages;
using Anchor.Web.Components.Pages.Admin;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Anchor.Web.Tests;

public sealed class LayoutAndPageRenderTests : BunitContext
{
    [Fact]
    public void MainLayout_TogglesBetweenLightAndDarkThemes()
    {
        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<section>Preview body</section>"))));

        Assert.Contains("theme-light", cut.Markup);
        Assert.DoesNotContain("site-header__nav-stack is-open", cut.Markup, StringComparison.Ordinal);

        cut.Find(".switch input").Change(true);

        Assert.Contains("theme-dark", cut.Markup);
        Assert.Contains("Preview body", cut.Markup);
        Assert.Equal("false", cut.Find(".preview-nav__link--account").GetAttribute("data-enhance-nav"));

        cut.Find(".menu-toggle").Click();

        Assert.Contains("site-header__nav-stack is-open", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void HomePage_RendersGuestWelcomeAndBuildingPlaceholder()
    {
        var cut = Render<Home>();

        Assert.Contains("Welcome aboard The Anchor.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Exterior Photo Placement", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Browse the Menu", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Monday Night Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sunday Pork Chop Dinner", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Thursday Trivia", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Summer Kickoff Patio Party", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MenuPage_RendersMenuSectionsFromMockupData()
    {
        var cut = Render<Menu>();

        Assert.Contains("Menu mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Appetizers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Monday Night Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sunday Pork Chop Dinner", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(cut.FindAll(".menu-item__image"));
        Assert.NotEmpty(cut.FindAll(".menu-item--text-only"));
        Assert.Contains("Coming Soon", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seasonal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Limited Time Special", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventsPage_RendersUpcomingCalendarContent()
    {
        var cut = Render<Events>();

        Assert.Contains("Events mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Thursday Trivia", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Friday Live Music", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Third Friday Steak Night", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Summer Kickoff Patio Party", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Community Bingo Fundraiser", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(cut.FindAll(".event-card__image"));
        Assert.Contains("Show every currently scheduled event in one public-facing list.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("every other Friday", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AboutPage_RendersStoryAndGuestExperienceSections()
    {
        var cut = Render<About>();

        Assert.Contains("About mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Story Direction", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Guest Experience", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContactPage_RendersHoursAndInquiryMockup()
    {
        var cut = Render<Contact>();

        Assert.Contains("Contact mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hours Preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Social Media", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Facebook", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Instagram", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(cut.FindAll(".social-profile__link"));
        Assert.Contains("Send Mockup Inquiry", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventsAdminPage_RendersHelpAndEditDeleteActions()
    {
        var cut = Render<EventsAdmin>();

        Assert.Contains("Events admin", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("How this page should work", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Event image (optional)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recurring event?", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recurrence pattern", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Repeat cadence", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Week of month", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recurs until (optional)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("every other Friday", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Third Friday Steak Night", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Choose an existing badge or type a new one to create it on the fly", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, cut.FindAll("input[type='date']").Count);
        Assert.Single(cut.FindAll("input[type='time']"));
        Assert.Contains("Delete", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MenuAdminPage_RendersMenuManagementPreview()
    {
        var cut = Render<MenuAdmin>();

        Assert.Contains("Menu admin", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Item editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Section preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Menu image (optional)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Optional image", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Choose an existing section or type a new section name to create it", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Offer start date", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, cut.FindAll("input[type='date']").Count);
        Assert.Contains("Recurring specials", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Day of week", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Monday Night Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seasonal item?", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("without an end date is not treated as seasonal or limited time", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AboutAdminPage_RendersContentBlockEditor()
    {
        var cut = Render<AboutAdmin>();

        Assert.Contains("About admin", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Content Blocks", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Current about sections", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContactAdminPage_RendersContactWorkflowPreview()
    {
        var cut = Render<ContactAdmin>();

        Assert.Contains("Contact admin", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Contact details form", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hours preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Social profiles", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Profile editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Choose an existing platform or type a new one", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Add another profile", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
