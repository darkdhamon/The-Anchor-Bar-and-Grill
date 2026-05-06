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

        cut.Find(".theme-toggle").Click();

        Assert.Contains("theme-dark", cut.Markup);
        Assert.Contains("Preview body", cut.Markup);
        Assert.Equal("false", cut.Find(".preview-nav__link--account").GetAttribute("data-enhance-nav"));
    }

    [Fact]
    public void HomePage_RendersGuestWelcomeAndBuildingPlaceholder()
    {
        var cut = Render<Home>();

        Assert.Contains("Welcome aboard The Anchor.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Exterior Photo Placement", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Browse the Menu", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MenuPage_RendersMenuSectionsFromMockupData()
    {
        var cut = Render<Menu>();

        Assert.Contains("Menu mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Appetizers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(cut.FindAll(".menu-item__image"));
        Assert.NotEmpty(cut.FindAll(".menu-item--text-only"));
        Assert.Contains("Coming Soon", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seasonal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Limited Time Special", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventsPage_RendersFeaturedCalendarContent()
    {
        var cut = Render<Events>();

        Assert.Contains("Events mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Thursday Trivia", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Use this page to explain the weekly rhythm.", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("Send Mockup Inquiry", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventsAdminPage_RendersHelpAndEditDeleteActions()
    {
        var cut = Render<EventsAdmin>();

        Assert.Contains("Events admin", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("How this page should work", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
    }
}
