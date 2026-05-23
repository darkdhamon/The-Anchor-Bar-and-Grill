using System.Security.Claims;
using Anchor.Domain.Identity;
using Anchor.Domain.Menu;
using Anchor.Web.Components.Pages.Admin;
using Anchor.Web.Images;
using Anchor.Web.Tests.Support;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Tests.Components.Pages.Admin;

public sealed class MenuAdminRedesignTests : BunitContext
{
    private readonly TestAuthenticationStateProvider authStateProvider;

    public MenuAdminRedesignTests()
    {
        var timeComboBoxModule = JSInterop.SetupModule("./Components/Shared/InputTimeComboBox.razor.js");
        timeComboBoxModule.SetupVoid("scrollRelevantOption", _ => true);

        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        Services.AddLogging();
        Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(ApplicationPolicies.AdminAccess, policy => policy.RequireRole(ApplicationRoles.Admin));
            options.AddPolicy(ApplicationPolicies.EventManagement, policy => policy.RequireRole(ApplicationRoles.EventManager));
            options.AddPolicy(ApplicationPolicies.MenuManagement, policy => policy.RequireRole(ApplicationRoles.MenuManager));
            options.AddPolicy(ApplicationPolicies.ITAccess, policy => policy.RequireRole(ApplicationRoles.It));
        });
        Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

        authStateProvider = new TestAuthenticationStateProvider();
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddCascadingAuthenticationState();

        Services.AddSingleton<IMenuQueryService>(new StaticMenuAdminQueryService());
        Services.AddSingleton<IMenuManagementService>(new StaticMenuAdminManagementService());
        Services.AddSingleton<IMenuItemImageStorage>(new StaticMenuItemImageStorage());
    }

    [Fact]
    public void Defaults_to_food_workspace_and_renders_top_level_tabs()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        Assert.Single(cut.FindAll(".menu-editor-nav"));
        var selectedTab = cut.FindAll(".menu-editor-tabs__button.is-selected").Single();

        Assert.Equal("Food", selectedTab.TextContent.Trim());
        Assert.Contains("Food browser", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("How to use this page", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hours", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Drinks", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Food_query_string_selects_requested_meal_filter()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=breakfast");
        ExpandBrowserSection(cut, "Breakfast Plates");

        var mealFilter = cut.FindAll(".menu-editor-filters")
            .Single(section => section.TextContent.Contains("Meal filter", StringComparison.OrdinalIgnoreCase));

        var selectedChip = mealFilter.GetElementsByTagName("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Breakfast", StringComparison.Ordinal));

        Assert.Equal("Breakfast", selectedChip.TextContent.Trim());
        Assert.Contains("is-selected", selectedChip.ClassName, StringComparison.Ordinal);
        Assert.Contains("Breakfast Burrito", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Late Night Burger", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Specials_content_filter_shows_special_items()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        cut.FindAll(".menu-editor-filter-chip")
            .Single(button => string.Equals(button.TextContent.Trim(), "Specials", StringComparison.Ordinal))
            .Click();
        ExpandAllBrowserSections(cut);

        Assert.Contains("Late Night Burger", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret Nachos", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Breakfast Burrito", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Empty_sections_stay_hidden_by_default_in_the_browser()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        var browserText = cut.Find(".menu-editor-tree").TextContent;

        Assert.DoesNotContain("Unassigned Platters", browserText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void New_food_sections_default_to_lunch_and_dinner_visibility()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add section", StringComparison.Ordinal))
            .Click();

        var detailButtons = cut.FindAll(".menu-editor-detail button");
        var breakfastChip = detailButtons.Single(button => string.Equals(button.TextContent.Trim(), "Breakfast", StringComparison.Ordinal));
        var lunchChip = detailButtons.Single(button => string.Equals(button.TextContent.Trim(), "Lunch", StringComparison.Ordinal));
        var dinnerChip = detailButtons.Single(button => string.Equals(button.TextContent.Trim(), "Dinner", StringComparison.Ordinal));

        Assert.DoesNotContain("is-selected", breakfastChip.ClassName, StringComparison.Ordinal);
        Assert.Contains("is-selected", lunchChip.ClassName, StringComparison.Ordinal);
        Assert.Contains("is-selected", dinnerChip.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void New_drink_sections_automatically_save_to_drinks()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        var captureService = new CapturingMenuAdminManagementService();
        Services.AddSingleton<IMenuManagementService>(captureService);

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add section", StringComparison.Ordinal))
            .Click();

        var createButton = cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create section", StringComparison.Ordinal));

        Assert.True(createButton.HasAttribute("disabled"));
        Assert.Contains("Drink sections automatically appear on the Drinks menu.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Drinks</button>", cut.Markup, StringComparison.OrdinalIgnoreCase);

        cut.Find("input[placeholder='Appetizers, Wings, Cocktails...']").Input("Mocktails");
        createButton = cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create section", StringComparison.Ordinal));

        Assert.False(createButton.HasAttribute("disabled"));

        createButton.Click();

        Assert.NotNull(captureService.LastSaveSectionRequest);
        Assert.Equal(MenuFamily.Drink, captureService.LastSaveSectionRequest!.Family);
        Assert.Equal([MenuTab.Drinks], captureService.LastSaveSectionRequest.MenuTabs);
    }

    [Fact]
    public void Parent_section_picker_only_offers_top_level_sections()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=breakfast");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add section", StringComparison.Ordinal))
            .Click();

        var parentSelect = cut.FindAll(".menu-editor-detail select")[1];
        var optionLabels = parentSelect.GetElementsByTagName("option")
            .Select(option => option.TextContent.Trim())
            .ToArray();

        Assert.Contains("Breakfast Plates", optionLabels);
        Assert.DoesNotContain("Omelets", optionLabels);
    }

    [Fact]
    public void Add_special_item_shows_weekday_chip_controls()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add special item", StringComparison.Ordinal))
            .Click();

        var detailButtons = cut.FindAll(".menu-editor-detail button");

        Assert.Contains("Special settings", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(detailButtons, button => string.Equals(button.TextContent.Trim(), "Monday", StringComparison.Ordinal));
        Assert.Contains(detailButtons, button => string.Equals(button.TextContent.Trim(), "Sunday", StringComparison.Ordinal));
        Assert.Contains(detailButtons, button => string.Equals(button.TextContent.Trim(), "Enable recurring season", StringComparison.Ordinal));
    }

    [Fact]
    public void Parent_section_cards_mix_subsections_and_items_in_one_ordered_stream()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=breakfast");

        var breakfastSection = cut.FindAll("article.menu-editor-tree__section")
            .Single(section => string.Equals(
                section.QuerySelector(":scope > .menu-editor-tree__row .menu-editor-tree__title")?.TextContent.Trim(),
                "Breakfast Plates",
                StringComparison.Ordinal));

        breakfastSection.QuerySelector(":scope > .menu-editor-tree__row")!.Click();

        breakfastSection = cut.FindAll("article.menu-editor-tree__section")
            .Single(section => string.Equals(
                section.QuerySelector(":scope > .menu-editor-tree__row .menu-editor-tree__title")?.TextContent.Trim(),
                "Breakfast Plates",
                StringComparison.Ordinal));

        var orderedRows = breakfastSection.QuerySelectorAll(".menu-editor-tree__group .menu-editor-tree__row .menu-editor-tree__title")
            .Select(title => title.TextContent.Trim())
            .ToArray();

        Assert.Equal(["Breakfast Burrito", "Omelets"], orderedRows);
        Assert.DoesNotContain("Subsections", breakfastSection.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Items", breakfastSection.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("How to use this page", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("menu-editor-guide", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Browser_sections_start_collapsed_by_default()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=breakfast");

        var breakfastSection = cut.FindAll("article.menu-editor-tree__section")
            .Single(section => string.Equals(
                section.QuerySelector(":scope > .menu-editor-tree__row .menu-editor-tree__title")?.TextContent.Trim(),
                "Breakfast Plates",
                StringComparison.Ordinal));

        Assert.DoesNotContain("is-expanded", breakfastSection.ClassName, StringComparison.Ordinal);
        Assert.Empty(breakfastSection.QuerySelectorAll(".menu-editor-tree__group"));
        Assert.Contains("Collapsed", breakfastSection.TextContent, StringComparison.Ordinal);
        Assert.Equal("false", breakfastSection.QuerySelector(":scope > .menu-editor-tree__row")?.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Both_filter_shows_archived_and_hidden_rows_with_different_states()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        cut.FindAll(".menu-editor-segmented__button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Both", StringComparison.Ordinal))
            .Click();
        ExpandBrowserSection(cut, "Appetizers");

        var archivedRow = cut.FindAll(".menu-editor-tree__row")
            .Single(row => row.TextContent.Contains("Retired Nachos", StringComparison.OrdinalIgnoreCase));

        var hiddenRow = cut.FindAll(".menu-editor-tree__row")
            .Single(row => row.TextContent.Contains("Secret Nachos", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("is-archived", archivedRow.ClassName, StringComparison.Ordinal);
        Assert.Contains("is-hidden", hiddenRow.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Archived_filter_keeps_active_parent_section_visible_when_archived_child_matches()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        cut.FindAll(".menu-editor-segmented__button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Archived", StringComparison.Ordinal))
            .Click();
        ExpandBrowserSection(cut, "Appetizers");

        var section = cut.FindAll(".menu-editor-tree__section")
            .Single(row => row.TextContent.Contains("Appetizers", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("is-context-muted", section.ClassName, StringComparison.Ordinal);
        Assert.Contains("Retired Nachos", section.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_item_uses_selected_section_as_context()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        var captureService = new CapturingMenuAdminManagementService();
        Services.AddSingleton<IMenuManagementService>(captureService);

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=breakfast");

        cut.FindAll(".menu-editor-tree__select")
            .First(button => button.TextContent.Contains("Breakfast Plates", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']").Input("Sunrise Stack");
        cut.FindAll(".menu-editor-price-row input")[0].Input("Regular");
        cut.FindAll(".menu-editor-price-row input")[1].Input("13.00");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create item", StringComparison.Ordinal))
            .Click();

        Assert.NotNull(captureService.LastSaveItemRequest);
        Assert.Single(captureService.LastSaveItemRequest!.SectionAssignments);
        Assert.Equal([MenuTab.Breakfast], captureService.LastSaveItemRequest.MenuTabs);
    }

    [Fact]
    public void Item_save_failures_render_inside_the_detail_panel()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        Services.AddSingleton<IMenuManagementService>(new FailingMenuAdminManagementService());

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Cocktails", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']").Input("Pepsi");
        cut.FindAll(".menu-editor-price-row input")[0].Input("Regular");
        cut.FindAll(".menu-editor-price-row input")[1].Input("3.00");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create item", StringComparison.Ordinal))
            .Click();

        var detailAlert = cut.Find(".menu-editor-detail .alert-danger");

        Assert.Contains("Description is optional", detailAlert.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_item_uses_current_textbox_values_without_needing_blur()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        var captureService = new CapturingMenuAdminManagementService();
        Services.AddSingleton<IMenuManagementService>(captureService);

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Cocktails", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']").Input("Pepsi");
        cut.FindAll(".menu-editor-price-row input")[0].Input("Regular");
        cut.FindAll(".menu-editor-price-row input")[1].Input("3.00");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create item", StringComparison.Ordinal))
            .Click();

        Assert.NotNull(captureService.LastSaveItemRequest);
        Assert.Equal("Pepsi", captureService.LastSaveItemRequest!.Name);
        Assert.Equal(string.Empty, captureService.LastSaveItemRequest.Description);
    }

    [Fact]
    public void Existing_item_accepts_change_style_description_updates()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        var captureService = new CapturingMenuAdminManagementService();
        Services.AddSingleton<IMenuManagementService>(captureService);

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");
        ExpandBrowserSection(cut, "Cocktails");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Old Fashioned", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.Find("textarea").Change("Voice typed description");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Save item", StringComparison.Ordinal))
            .Click();

        Assert.NotNull(captureService.LastSaveItemRequest);
        Assert.Equal("Voice typed description", captureService.LastSaveItemRequest!.Description);
    }

    [Fact]
    public void Existing_item_preserves_price_variant_ids_when_adding_a_second_price()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        var captureService = new CapturingMenuAdminManagementService();
        Services.AddSingleton<IMenuManagementService>(captureService);

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");
        ExpandBrowserSection(cut, "Cocktails");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Old Fashioned", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add price variant", StringComparison.Ordinal))
            .Click();

        var priceInputs = cut.FindAll(".menu-editor-price-row input");
        priceInputs[2].Input("Tall");
        priceInputs[3].Input("14.00");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Save item", StringComparison.Ordinal))
            .Click();

        Assert.NotNull(captureService.LastSaveItemRequest);
        Assert.Equal(2, captureService.LastSaveItemRequest!.PriceVariants.Count);
        Assert.Equal(StaticMenuAdminQueryService.DrinkItemPriceVariantId, captureService.LastSaveItemRequest.PriceVariants[0].PriceVariantId);
        Assert.Null(captureService.LastSaveItemRequest.PriceVariants[1].PriceVariantId);
    }

    [Fact]
    public void Item_editor_shows_image_thumbnail_and_preview_modal_markup()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");
        ExpandBrowserSection(cut, "Cocktails");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Old Fashioned", StringComparison.OrdinalIgnoreCase))
            .Click();

        Assert.NotNull(cut.Find(".menu-editor-image-preview__trigger"));
        Assert.Contains("image-preview-modal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.True(cut.Find(".image-preview-modal-backdrop").HasAttribute("hidden"));
        Assert.Contains("/images/menu/wings.svg", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Item_upload_updates_image_path_immediately_before_save()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        var captureService = new CapturingMenuAdminManagementService();
        var imageStorage = new CapturingMenuItemImageStorage("/images/gallery/menuitems/mocktail.webp");
        Services.AddSingleton<IMenuManagementService>(captureService);
        Services.AddSingleton<IMenuItemImageStorage>(imageStorage);

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Cocktails", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']").Input("Cranberry Spritz");
        cut.FindAll(".menu-editor-price-row input")[0].Input("Regular");
        cut.FindAll(".menu-editor-price-row input")[1].Input("6.00");

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromBinary([0x01, 0x02, 0x03], "mocktail.png", contentType: "image/png"));

        Assert.Equal("mocktail.png", imageStorage.LastFileName);
        Assert.Contains("Image uploaded.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/images/gallery/menuitems/mocktail.webp", cut.Markup, StringComparison.OrdinalIgnoreCase);

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create item", StringComparison.Ordinal))
            .Click();

        Assert.NotNull(captureService.LastSaveItemRequest);
        Assert.Equal("/images/gallery/menuitems/mocktail.webp", captureService.LastSaveItemRequest!.ImagePath);
    }

    [Fact]
    public void Duplicate_item_name_blur_prompts_to_edit_the_existing_item()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Cocktails", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        var nameInput = cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']");
        nameInput.Input("Old Fashioned");
        nameInput.TriggerEvent("onblur", new FocusEventArgs());

        cut.WaitForAssertion(() => Assert.Contains("Would you like to edit the existing item instead?", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Edit existing item", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Contains("Edit item: Old Fashioned", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Thrown_item_save_errors_render_as_inline_status_messages()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        Services.AddSingleton<IMenuManagementService>(new ThrowingMenuAdminManagementService());

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Cocktails", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']").Input("Pepsi");
        cut.FindAll(".menu-editor-price-row input")[0].Input("Regular");
        cut.FindAll(".menu-editor-price-row input")[1].Input("3.00");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create item", StringComparison.Ordinal))
            .Click();

        var detailAlert = cut.Find(".menu-editor-detail .alert-danger");

        Assert.Contains("couldn't save the menu item", detailAlert.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    private IRenderedComponent<ContainerFragment> RenderMenuAdmin(string uri)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

        return Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MenuAdmin>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });
    }

    private static void ExpandAllBrowserSections(IRenderedComponent<ContainerFragment> cut)
    {
        foreach (var section in cut.FindAll("article.menu-editor-tree__section").ToArray())
        {
            var header = section.QuerySelector(":scope > .menu-editor-tree__row");
            if (header is not null
                && string.Equals(header.GetAttribute("aria-expanded"), "false", StringComparison.Ordinal))
            {
                header.Click();
            }
        }
    }

    private static void ExpandBrowserSection(IRenderedComponent<ContainerFragment> cut, string sectionTitle)
    {
        var section = cut.FindAll("article.menu-editor-tree__section")
            .Single(card => string.Equals(
                card.QuerySelector(":scope > .menu-editor-tree__row .menu-editor-tree__title")?.TextContent.Trim(),
                sectionTitle,
                StringComparison.Ordinal));

        var header = section.QuerySelector(":scope > .menu-editor-tree__row");

        if (header is not null
            && string.Equals(header.GetAttribute("aria-expanded"), "false", StringComparison.Ordinal))
        {
            header.Click();
        }
    }

    private static ClaimsPrincipal CreateUser(string userName, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.NameIdentifier, userName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
    }

    private sealed class StaticMenuAdminQueryService : IMenuQueryService
    {
        internal static readonly Guid AppetizersSectionId = Guid.Parse("D8F92296-4F3C-4B88-B2D4-D1775F54A1D1");
        private static readonly Guid BreakfastSectionId = Guid.Parse("8A88226D-F45A-4E15-9420-8E1828654A73");
        private static readonly Guid OmeletsSectionId = Guid.Parse("A2FCA86D-BBF5-490D-84FB-A5D08A21F89F");
        private static readonly Guid CocktailsSectionId = Guid.Parse("5E3C8768-2020-4C8A-A565-B2B981AAB1B1");
        private static readonly Guid EmptyFoodSectionId = Guid.Parse("266FAF80-C3BA-4D60-BD70-7B2224D52671");
        private static readonly Guid ActiveSpecialItemId = Guid.Parse("A4CC9DA8-54AE-4FA9-85D1-2E666FCF4B18");
        private static readonly Guid HiddenFoodItemId = Guid.Parse("89CE687D-62E8-453F-8D08-12D74F85FCB9");
        private static readonly Guid ArchivedFoodItemId = Guid.Parse("44AA62BE-4B4D-46C7-A3D3-5088BF3B58DD");
        private static readonly Guid BreakfastItemId = Guid.Parse("797FEE70-BA14-46A5-AB88-DCDA3DAF7262");
        private static readonly Guid OmeletItemId = Guid.Parse("65D6966E-0DC9-43F3-8A97-41DDFE372B67");
        private static readonly Guid DrinkItemId = Guid.Parse("0A5B6B42-778E-49D3-8568-9AB1A785432D");
        internal static readonly Guid DrinkItemPriceVariantId = Guid.Parse("B81338F8-E82A-4B8A-B9B0-2D831B2C5520");
        private static readonly DateOnly Today = new(2026, 5, 18);

        public Task<MenuTab> GetSuggestedPublicTabAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicHomeSpecialView>>([]);

        public Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MenuTabHoursAdminView> tabs = Enum.GetValues<MenuTab>()
                .Select(tab => new MenuTabHoursAdminView(
                    tab,
                    tab.ToString(),
                    new[]
                    {
                        new MenuServiceWindowView(DayOfWeek.Monday, "Monday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Tuesday, "Tuesday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Tuesday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Wednesday, "Wednesday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Wednesday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Thursday, "Thursday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Thursday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Friday, "Friday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Friday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Saturday, "Saturday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Saturday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Sunday, "Sunday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Sunday, new TimeOnly(11, 0), new TimeOnly(17, 0), false)
                    }))
                .ToArray();

            IReadOnlyList<MenuSectionAdminView> sections =
            [
                MenuAdminViewFactory.Section(AppetizersSectionId, "Appetizers", MenuFamily.Food, [MenuTab.Lunch, MenuTab.Dinner], 1, callout: "Shareables for the table."),
                MenuAdminViewFactory.Section(BreakfastSectionId, "Breakfast Plates", MenuFamily.Food, [MenuTab.Breakfast], 2),
                MenuAdminViewFactory.Section(OmeletsSectionId, "Omelets", MenuFamily.Food, [MenuTab.Breakfast], 11, parentSectionId: BreakfastSectionId, parentSectionName: "Breakfast Plates"),
                MenuAdminViewFactory.Section(EmptyFoodSectionId, "Unassigned Platters", MenuFamily.Food, [MenuTab.Lunch], 3),
                MenuAdminViewFactory.Section(CocktailsSectionId, "Cocktails", MenuFamily.Drink, [MenuTab.Drinks], 1)
            ];

            IReadOnlyList<MenuItemAdminView> items =
            [
                MenuAdminViewFactory.Item(
                    ActiveSpecialItemId,
                    MenuFamily.Food,
                    "Late Night Burger",
                    "Lunch and dinner burger.",
                    1,
                    [MenuAdminViewFactory.Assignment(AppetizersSectionId, "Appetizers", 1)],
                    [MenuTab.Lunch, MenuTab.Dinner],
                    [new MenuItemPriceVariantView("Regular", 12m, 1)],
                    ["Special", "Today"],
                    null,
                    new MenuItemSpecialAdminView(
                        MenuItemSpecialScheduleKind.WeeklyRecurring,
                        DayOfWeek.Monday,
                        new DateOnly(2026, 1, 1),
                        null,
                        new TimeOnly(17, 0),
                        null,
                        false,
                        "Monday",
                        "Every Monday starting Jan 1",
                        "After 5:00 PM",
                        "$11 basket special",
                        new[] { "Today" },
                        Today.DayOfWeek == DayOfWeek.Monday)),
                MenuAdminViewFactory.Item(
                    HiddenFoodItemId,
                    MenuFamily.Food,
                    "Secret Nachos",
                    "Hidden test item.",
                    2,
                    [MenuAdminViewFactory.Assignment(AppetizersSectionId, "Appetizers", 2)],
                    [MenuTab.Lunch],
                    [new MenuItemPriceVariantView("Regular", 10m, 1)],
                    ["Hidden"],
                    isVisibleToGuests: false),
                MenuAdminViewFactory.Item(
                    ArchivedFoodItemId,
                    MenuFamily.Food,
                    "Retired Nachos",
                    "Archived test item.",
                    3,
                    [MenuAdminViewFactory.Assignment(AppetizersSectionId, "Appetizers", 3)],
                    [MenuTab.Lunch],
                    [new MenuItemPriceVariantView("Regular", 9m, 1)],
                    ["Archived"],
                    isArchived: true),
                MenuAdminViewFactory.Item(
                    BreakfastItemId,
                    MenuFamily.Food,
                    "Breakfast Burrito",
                    "Breakfast-only item.",
                    1,
                    [MenuAdminViewFactory.Assignment(BreakfastSectionId, "Breakfast Plates", 1)],
                    [MenuTab.Breakfast],
                    [new MenuItemPriceVariantView("Regular", 11m, 1)]),
                MenuAdminViewFactory.Item(
                    OmeletItemId,
                    MenuFamily.Food,
                    "Denver Omelet",
                    "Child section breakfast item.",
                    1,
                    [MenuAdminViewFactory.Assignment(OmeletsSectionId, "Omelets", 1)],
                    [MenuTab.Breakfast],
                    [new MenuItemPriceVariantView("Regular", 12m, 1)]),
                MenuAdminViewFactory.Item(
                    DrinkItemId,
                    MenuFamily.Drink,
                    "Old Fashioned",
                    "Drink item.",
                    1,
                    [MenuAdminViewFactory.Assignment(CocktailsSectionId, "Cocktails", 1)],
                    [MenuTab.Drinks],
                    [new MenuItemPriceVariantView(DrinkItemPriceVariantId, "Regular", 12m, 1)],
                    imagePath: "images/menu/wings.svg")
            ];

            return Task.FromResult(new MenuManagementView(tabs, sections, items));
        }
    }

    private sealed class StaticMenuAdminManagementService : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.ItemId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class StaticMenuItemImageStorage : IMenuItemImageStorage
    {
        public Task<string> SaveImageAsync(
            Stream source,
            string originalFileName,
            string? contentType,
            long declaredLength,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("/images/gallery/menuitems/mock-image.webp");
    }

    private sealed class CapturingMenuItemImageStorage(string savedPath) : IMenuItemImageStorage
    {
        public string? LastFileName { get; private set; }

        public Task<string> SaveImageAsync(
            Stream source,
            string originalFileName,
            string? contentType,
            long declaredLength,
            CancellationToken cancellationToken = default)
        {
            LastFileName = originalFileName;
            return Task.FromResult(savedPath);
        }
    }

    private sealed class FailingMenuAdminManagementService : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Failure("Description is optional. This is the inline item-save test failure."));

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class CapturingMenuAdminManagementService : IMenuManagementService
    {
        public SaveMenuSectionRequest? LastSaveSectionRequest { get; private set; }
        public SaveMenuItemRequest? LastSaveItemRequest { get; private set; }

        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default)
        {
            LastSaveSectionRequest = request;
            return Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));
        }

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default)
        {
            LastSaveItemRequest = request;
            return Task.FromResult(MenuOperationResult.Success(request.ItemId ?? Guid.NewGuid()));
        }

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class ThrowingMenuAdminManagementService : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Test exception");

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private AuthenticationState authenticationState = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(authenticationState);

        public void SetUser(ClaimsPrincipal user)
        {
            authenticationState = new AuthenticationState(user);
            NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
        }
    }

    private sealed class TestAuthorizationService(IAuthorizationPolicyProvider policyProvider) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(Evaluate(user, requirements));

        public async Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName)
        {
            var policy = await policyProvider.GetPolicyAsync(policyName);
            return policy is null
                ? AuthorizationResult.Failed()
                : Evaluate(user, policy.Requirements);
        }

        private static AuthorizationResult Evaluate(
            ClaimsPrincipal user,
            IEnumerable<IAuthorizationRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                switch (requirement)
                {
                    case DenyAnonymousAuthorizationRequirement when user.Identity?.IsAuthenticated != true:
                        return AuthorizationResult.Failed();

                    case RolesAuthorizationRequirement rolesRequirement
                        when !rolesRequirement.AllowedRoles.Any(user.IsInRole):
                        return AuthorizationResult.Failed();
                }
            }

            return AuthorizationResult.Success();
        }
    }
}
