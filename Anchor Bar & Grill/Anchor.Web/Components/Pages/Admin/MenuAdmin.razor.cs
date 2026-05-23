using System.Globalization;
using Anchor.Domain.Menu;
using Anchor.Web.Components.Pages;
using Anchor.Web.Components.Shared;
using Anchor.Web.Images;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Anchor.Web.Components.Pages.Admin;

public partial class MenuAdmin
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly DayOfWeek[] OrderedDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    private readonly DateOnly today = DateOnly.FromDateTime(DateTime.Today);
    private MenuManagementView? menuView;
    private MenuSectionFormModel sectionForm = new();
    private MenuItemFormModel itemForm = CreateDefaultItemForm();
    private MenuServiceWindowFormModel serviceWindowForm = new();
    private string? statusMessage;
    private bool isLoading = true;
    private MenuEditorTab selectedEditorTab = MenuEditorTab.Food;
    private MenuTab? selectedFoodFilter;
    private MenuArchiveFilter foodArchiveFilter = MenuArchiveFilter.Active;
    private MenuArchiveFilter drinkArchiveFilter = MenuArchiveFilter.Active;
    private MenuContentFilter foodContentFilter = MenuContentFilter.All;
    private MenuContentFilter drinkContentFilter = MenuContentFilter.All;
    private MenuAdminDetailKind detailKind;
    private Guid? selectedBrowserId;
    private Guid? pendingSectionDeleteId;
    private Guid? pendingItemDeleteId;
    private MenuBrowserDragState? dragState;
    private readonly HashSet<Guid> expandedBrowserSectionIds = [];
    private string sectionSnapshot = string.Empty;
    private string itemSnapshot = string.Empty;
    private string hoursSnapshot = string.Empty;
    private readonly HashSet<Guid> sessionVisibleEmptySectionIds = [];
    private MenuAdminDuplicateItemPromptViewModel? duplicateItemPrompt;
    private string? itemImageUploadStatusMessage;
    private string? itemImageUploadErrorMessage;
    private bool isItemImageUploading;

    [Inject]
    private IMenuQueryService MenuQueryService { get; set; } = null!;

    [Inject]
    private IMenuManagementService MenuManagementService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<MenuAdmin> Logger { get; set; } = null!;

    [Inject]
    private IMenuItemImageStorage MenuItemImageStorage { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "tab")]
    private string? RequestedEditorTab { get; set; }

    [SupplyParameterFromQuery(Name = "food")]
    private string? RequestedFoodFilter { get; set; }

    private IReadOnlyList<MenuSectionAdminView> Sections => menuView?.Sections ?? [];

    private IReadOnlyList<MenuItemAdminView> Items => menuView?.Items ?? [];

    private IReadOnlyList<MenuTabHoursAdminView> HourTabs => menuView?.Tabs ?? [];

    private MenuFamily CurrentContentFamily => selectedEditorTab == MenuEditorTab.Drinks ? MenuFamily.Drink : MenuFamily.Food;

    private string CurrentWorkspaceLabel => selectedEditorTab == MenuEditorTab.Drinks ? "Drinks" : "Food";

    private IReadOnlyList<MenuAdminBrowserSectionViewModel> BrowserSections =>
        selectedEditorTab == MenuEditorTab.Drinks
            ? BuildBrowserSections(MenuFamily.Drink, null, drinkArchiveFilter, drinkContentFilter)
            : BuildBrowserSections(MenuFamily.Food, selectedFoodFilter, foodArchiveFilter, foodContentFilter);

    private IReadOnlyList<MenuAdminHoursSummaryViewModel> HoursSummaryCards =>
        HourTabs
            .OrderBy(tab => tab.Tab)
            .Select(tab => new MenuAdminHoursSummaryViewModel(tab.Tab, tab.Label, MenuHoursPresentation.Create(tab.Days)))
            .ToArray();

    private IReadOnlyList<MenuSectionAdminView> ItemSectionOptions =>
        Sections
            .Where(section => section.Family == GetItemEditorFamily())
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<MenuSectionAdminView> ParentSectionOptions =>
        Sections
            .Where(section => section.Family == sectionForm.Family)
            .Where(section => section.SectionId != sectionForm.SectionId)
            .Where(section => section.ParentSectionId is null)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<(int Value, string Label)> MonthOptions { get; } =
        Enumerable.Range(1, 12)
            .Select(month => (month, CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)))
            .ToArray();

    private bool HasItemCreationContext => Sections.Any(section => section.Family == CurrentContentFamily);

    private bool SectionHasUnsavedChanges => BuildSectionSnapshot(sectionForm) != sectionSnapshot;

    private bool ItemHasUnsavedChanges => BuildItemSnapshot(itemForm) != itemSnapshot;

    private bool HoursHaveUnsavedChanges => BuildHoursSnapshot(serviceWindowForm) != hoursSnapshot;

    private bool HoursHaveValidationErrors => !TryValidateHoursForm(serviceWindowForm, out _);

    private bool CanSaveHours => HoursHaveUnsavedChanges && !HoursHaveValidationErrors;
    private string? CurrentItemImagePreviewPath => MenuImagePathDisplay.Normalize(itemForm.ImagePath);

    private string HoursEditorStateLabel =>
        !HoursHaveUnsavedChanges
            ? "Saved state"
            : CanSaveHours
                ? "Ready to save"
                : "Complete required times";

    private string? DetailStatusMessage => selectedEditorTab == MenuEditorTab.Hours ? null : statusMessage;
    private bool SectionHasAnyVisibleMenu => sectionForm.Family == MenuFamily.Drink
        ? sectionForm.ShowDrinks
        : sectionForm.ShowBreakfast || sectionForm.ShowLunch || sectionForm.ShowDinner;
    private bool CanSaveSection => !string.IsNullOrWhiteSpace(sectionForm.Name) && SectionHasAnyVisibleMenu;

    private MenuAdminDuplicateItemPromptViewModel? DuplicateItemPrompt => detailKind == MenuAdminDetailKind.Item ? duplicateItemPrompt : null;

    private bool CurrentDetailHasUnsavedChanges =>
        detailKind switch
        {
            MenuAdminDetailKind.Section => SectionHasUnsavedChanges,
            MenuAdminDetailKind.Item => ItemHasUnsavedChanges,
            _ => false
        };

    private string CurrentDetailTitle =>
        detailKind switch
        {
            MenuAdminDetailKind.Section => sectionForm.SectionId is null ? $"New {GetFamilyLabel(CurrentContentFamily)} section" : $"Edit section: {sectionForm.Name}",
            MenuAdminDetailKind.Item when itemForm.ItemId is null && itemForm.IsSpecial => $"New {GetFamilyLabel(CurrentContentFamily)} special item",
            MenuAdminDetailKind.Item when itemForm.ItemId is null => $"New {GetFamilyLabel(CurrentContentFamily)} item",
            MenuAdminDetailKind.Item => $"Edit item: {itemForm.Name}",
            _ => $"Select {GetFamilyLabel(CurrentContentFamily).ToLowerInvariant()} content"
        };

    protected override async Task OnParametersSetAsync()
    {
        selectedEditorTab = ParseEditorTab(RequestedEditorTab);
        selectedFoodFilter = ParseFoodFilter(RequestedFoodFilter);

        if (menuView is null)
        {
            await LoadAsync();
            ResetSectionForm();
            ResetItemForm();
            ResetHoursForm();
        }

        EnsureSelectionForCurrentTab();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        menuView = await MenuQueryService.GetMenuManagementViewAsync(today);
        sessionVisibleEmptySectionIds.RemoveWhere(sectionId => Sections.All(section => section.SectionId != sectionId));
        isLoading = false;
    }

    private void SelectEditorTab(MenuEditorTab tab)
    {
        selectedEditorTab = tab;
        EnsureSelectionForCurrentTab();
        UpdateLocation();
    }

    private void SelectFoodFilter(MenuTab? tab)
    {
        selectedFoodFilter = tab;
        EnsureSelectionForCurrentTab();
        UpdateLocation();
    }

    private void SetContentFilter(MenuContentFilter filter)
    {
        if (selectedEditorTab == MenuEditorTab.Drinks)
        {
            drinkContentFilter = filter;
        }
        else
        {
            foodContentFilter = filter;
        }

        EnsureSelectionForCurrentTab();
    }

    private void SetArchiveFilter(MenuArchiveFilter filter)
    {
        if (selectedEditorTab == MenuEditorTab.Drinks)
        {
            drinkArchiveFilter = filter;
        }
        else
        {
            foodArchiveFilter = filter;
        }

        EnsureSelectionForCurrentTab();
    }

    private void UpdateLocation()
    {
        var parameters = new Dictionary<string, object?>
        {
            ["tab"] = GetEditorTabQueryValue(selectedEditorTab),
            ["food"] = selectedFoodFilter is null ? "all" : GetTabQueryValue(selectedFoodFilter.Value)
        };

        var uri = NavigationManager.GetUriWithQueryParameters(
            NavigationManager.ToAbsoluteUri("/admin/menu").AbsoluteUri,
            parameters);

        NavigationManager.NavigateTo(uri, replace: true);
    }

    private void EnsureSelectionForCurrentTab()
    {
        if (selectedEditorTab == MenuEditorTab.Hours)
        {
            return;
        }

        if (IsCreatingNewRecord() && CurrentDetailBelongsToCurrentTab())
        {
            return;
        }

        if (!CurrentDetailBelongsToCurrentTab() || !CurrentDetailVisibleInBrowser())
        {
            SelectFirstVisibleRecordOrStartNew();
        }
    }

    private bool CurrentDetailBelongsToCurrentTab() =>
        detailKind switch
        {
            MenuAdminDetailKind.None => false,
            MenuAdminDetailKind.Section => GetSectionEditorFamily() == CurrentContentFamily,
            MenuAdminDetailKind.Item => GetItemEditorFamily() == CurrentContentFamily,
            _ => false
        };

    private bool CurrentDetailVisibleInBrowser() =>
        detailKind switch
        {
            MenuAdminDetailKind.Section when sectionForm.SectionId is Guid sectionId => BrowserSections.Any(section => section.Section.SectionId == sectionId),
            MenuAdminDetailKind.Item when itemForm.ItemId is Guid itemId => BrowserSections.Any(section => section.Items.Any(item => item.Item.ItemId == itemId)),
            _ => false
        };

    private bool IsCreatingNewRecord() =>
        detailKind switch
        {
            MenuAdminDetailKind.Section => sectionForm.SectionId is null,
            MenuAdminDetailKind.Item => itemForm.ItemId is null,
            _ => false
        };

    private void SelectFirstVisibleRecordOrStartNew()
    {
        var firstSection = BrowserSections.FirstOrDefault();
        if (firstSection is not null)
        {
            SelectSection(firstSection.Section);
            return;
        }

        StartNewSection(CurrentContentFamily);
    }

    private void ResetSectionForm(MenuFamily? preferredFamily = null)
    {
        sectionForm = new MenuSectionFormModel
        {
            Family = preferredFamily ?? MenuFamily.Food
        };

        NormalizeSectionFormForSelectedFamily();
        CaptureSectionSnapshot();
    }

    private void ResetItemForm()
    {
        ClearDuplicateItemPrompt();
        ClearItemImageFeedback();
        itemForm = CreateDefaultItemForm();
        var defaultSectionId = Sections
            .Where(section => section.Family == MenuFamily.Food)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .Select(section => section.SectionId)
            .FirstOrDefault();
        if (defaultSectionId != Guid.Empty)
        {
            itemForm.SelectedSectionIds.Add(defaultSectionId);
            itemForm.SectionSortOrders[defaultSectionId] = 1;
            itemForm.ActiveSectionId = defaultSectionId;
        }

        NormalizeItemFormForSelectedSections();
        itemForm.SortOrder = GetCurrentItemSortOrder();
        CaptureItemSnapshot();
    }

    private void ResetHoursForm(MenuTab? preferredTab = null)
    {
        var selectedTab = preferredTab ?? serviceWindowForm.Tab;
        if (!HourTabs.Any(tab => tab.Tab == selectedTab))
        {
            selectedTab = HourTabs.FirstOrDefault()?.Tab ?? MenuTab.Lunch;
        }

        serviceWindowForm = BuildHoursForm(selectedTab);
        CaptureHoursSnapshot();
    }

    private MenuServiceWindowFormModel BuildHoursForm(MenuTab tab)
    {
        var matchingTab = HourTabs.SingleOrDefault(hourTab => hourTab.Tab == tab);
        var form = new MenuServiceWindowFormModel
        {
            Tab = tab
        };

        foreach (var day in OrderedDays)
        {
            var window = matchingTab?.Days.SingleOrDefault(item => item.DayOfWeek == day);
            form.Days.Add(new MenuServiceWindowDayFormModel
            {
                DayOfWeek = day,
                IsAvailable = window?.IsAvailable ?? false,
                OpensAtText = FormatTime(window?.OpensAt),
                ClosesAtText = FormatTime(window?.ClosesAt),
                ClosesNextDay = window?.ClosesNextDay ?? false
            });
        }

        return form;
    }

    private void StartNewSection(MenuFamily family)
    {
        ClearPendingDeletes();
        sectionForm = new MenuSectionFormModel
        {
            Family = family,
            SortOrder = Sections
                .Where(section => section.Family == family)
                .Select(section => section.SortOrder)
                .DefaultIfEmpty(0)
                .Max() + 1
        };

        NormalizeSectionFormForSelectedFamily();
        detailKind = MenuAdminDetailKind.Section;
        selectedBrowserId = null;
        ClearDuplicateItemPrompt();
        CaptureSectionSnapshot();
    }

    private void StartNewSectionForCurrentTab() => StartNewSection(CurrentContentFamily);

    private void StartNewItem(MenuFamily family, bool isSpecial)
    {
        ClearPendingDeletes();
        ClearDuplicateItemPrompt();
        ClearItemImageFeedback();
        itemForm = CreateDefaultItemForm(isSpecial);

        var contextSection = ResolveContextSection(family);
        if (contextSection is not null)
        {
            itemForm.SelectedSectionIds.Add(contextSection.SectionId);
            itemForm.ActiveSectionId = contextSection.SectionId;
            itemForm.SectionSortOrders[contextSection.SectionId] = GetNextItemSortOrder(contextSection.SectionId, isSpecial);
        }

        itemForm.SortOrder = GetCurrentItemSortOrder();

        ApplyFoodFilterDefaultsToItemForm();
        NormalizeItemFormForSelectedSections();
        detailKind = MenuAdminDetailKind.Item;
        selectedBrowserId = null;
        CaptureItemSnapshot();
    }

    private void StartNewItemForCurrentTab() => StartNewItem(CurrentContentFamily, isSpecial: false);

    private void StartNewSpecialItemForCurrentTab() => StartNewItem(CurrentContentFamily, isSpecial: true);

    private void StartNewItemFromSection(MenuSectionAdminView section, bool isSpecial)
    {
        SelectSection(section);
        StartNewItem(section.Family, isSpecial);
    }

    private void ApplyFoodFilterDefaultsToItemForm()
    {
        if (GetItemEditorFamily() == MenuFamily.Drink)
        {
            itemForm.ShowBreakfast = false;
            itemForm.ShowLunch = false;
            itemForm.ShowDinner = false;
            return;
        }

        itemForm.ShowBreakfast = false;
        itemForm.ShowLunch = false;
        itemForm.ShowDinner = false;

        switch (selectedFoodFilter)
        {
            case MenuTab.Breakfast:
                itemForm.ShowBreakfast = true;
                break;
            case MenuTab.Lunch:
                itemForm.ShowLunch = true;
                break;
            case MenuTab.Dinner:
                itemForm.ShowDinner = true;
                break;
            default:
                itemForm.ShowLunch = true;
                itemForm.ShowDinner = true;
                break;
        }
    }

    private MenuSectionAdminView? ResolveContextSection(MenuFamily family)
    {
        Guid? sectionId = detailKind switch
        {
            MenuAdminDetailKind.Section when sectionForm.SectionId is Guid currentSectionId && GetSectionEditorFamily() == family => currentSectionId,
            MenuAdminDetailKind.Item when itemForm.ActiveSectionId is Guid activeSectionId => activeSectionId,
            MenuAdminDetailKind.Item => itemForm.SelectedSectionIds.OrderBy(id => id).FirstOrDefault(),
            _ => null
        };

        var candidate = sectionId is Guid id
            ? Sections.SingleOrDefault(section => section.SectionId == id && section.Family == family)
            : null;

        return candidate
            ?? Sections
                .Where(section => section.Family == family)
                .OrderBy(section => section.SortOrder)
                .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
    }

    private int GetNextItemSortOrder(Guid? sectionId, bool isSpecial) =>
        sectionId is Guid id
            ? Items
                .Where(item => item.SectionAssignments.Any(assignment => assignment.SectionId == id) && (item.Special is not null) == isSpecial)
                .Select(item => GetSectionAssignmentSortOrder(item, id))
                .DefaultIfEmpty(0)
                .Max() + 1
            : 1;

    private void SelectSection(MenuSectionAdminView section)
    {
        ClearPendingDeletes();
        ClearDuplicateItemPrompt();
        detailKind = MenuAdminDetailKind.Section;
        selectedBrowserId = section.SectionId;
        sectionForm = new MenuSectionFormModel
        {
            SectionId = section.SectionId,
            Name = section.Name,
            Callout = section.Callout,
            Family = section.Family,
            ParentSectionId = section.ParentSectionId,
            ShowBreakfast = section.MenuTabs.Contains(MenuTab.Breakfast),
            ShowLunch = section.MenuTabs.Contains(MenuTab.Lunch),
            ShowDinner = section.MenuTabs.Contains(MenuTab.Dinner),
            ShowDrinks = section.MenuTabs.Contains(MenuTab.Drinks),
            SortOrder = section.SortOrder,
            IsVisibleToGuests = section.IsVisibleToGuests,
            IsArchived = section.IsArchived
        };

        NormalizeSectionFormForSelectedFamily();
        CaptureSectionSnapshot();
    }

    private void SelectItem(MenuAdminBrowserItemViewModel itemEntry) => SelectItem(itemEntry.Item, itemEntry.SectionId);

    private void SelectItem(MenuItemAdminView item, Guid? activeSectionId = null)
    {
        ClearPendingDeletes();
        ClearDuplicateItemPrompt();
        ClearItemImageFeedback();
        detailKind = MenuAdminDetailKind.Item;
        selectedBrowserId = item.ItemId;
        itemForm = new MenuItemFormModel
        {
            ItemId = item.ItemId,
            Name = item.Name,
            Description = item.Description,
            ImagePath = item.ImagePath,
            IsVisibleToGuests = item.IsVisibleToGuests,
            IsArchived = item.IsArchived,
            OfferStartsOnText = FormatDate(item.OfferStartsOn),
            OfferEndsOnText = FormatDate(item.OfferEndsOn),
            IsSeasonal = item.IsSeasonal,
            UseRecurringSeasonWindow = item.SeasonStartMonth is not null && item.SeasonEndMonth is not null,
            SeasonStartMonth = item.SeasonStartMonth,
            SeasonStartDay = item.SeasonStartDay,
            SeasonEndMonth = item.SeasonEndMonth,
            SeasonEndDay = item.SeasonEndDay,
            UseSectionVisibility = item.UsesSectionVisibility,
            ShowBreakfast = item.MenuTabs.Contains(MenuTab.Breakfast),
            ShowLunch = item.MenuTabs.Contains(MenuTab.Lunch),
            ShowDinner = item.MenuTabs.Contains(MenuTab.Dinner),
            IsSpecial = item.Special is not null,
            SpecialScheduleKind = item.Special?.ScheduleKind ?? MenuItemSpecialScheduleKind.WeeklyRecurring,
            SpecialStartDateText = FormatDate(item.Special?.StartDate),
            SpecialEndDateText = FormatDate(item.Special?.EndDate),
            SpecialStartsAtText = FormatTime(item.Special?.StartsAt),
            SpecialEndsAtText = FormatTime(item.Special?.EndsAt),
            SpecialClosesNextDay = item.Special?.ClosesNextDay ?? false,
            SpecialCallout = item.Special?.Callout
        };

        if (item.Special is not null)
        {
            foreach (var day in item.Special.DaysOfWeek)
            {
                itemForm.SelectedSpecialDays.Add(day);
            }
        }

        foreach (var assignment in item.SectionAssignments)
        {
            itemForm.SelectedSectionIds.Add(assignment.SectionId);
            itemForm.SectionSortOrders[assignment.SectionId] = assignment.SortOrder;
        }

        itemForm.ActiveSectionId = activeSectionId is Guid requestedSectionId
            && item.SectionAssignments.Any(assignment => assignment.SectionId == requestedSectionId)
                ? requestedSectionId
                : item.SectionAssignments.OrderBy(assignment => assignment.SortOrder).Select(assignment => (Guid?)assignment.SectionId).FirstOrDefault();
        itemForm.SortOrder = GetCurrentItemSortOrder(item.SortOrder);

        itemForm.PriceVariants.Clear();
        foreach (var variant in item.PriceVariants)
        {
            itemForm.PriceVariants.Add(new MenuItemPriceVariantFormModel
            {
                PriceVariantId = variant.PriceVariantId,
                Label = variant.Label,
                AmountText = variant.Amount.ToString("0.00", InvariantCulture),
                SortOrder = variant.SortOrder
            });
        }

        NormalizeItemFormForSelectedSections();
        CaptureItemSnapshot();
    }

    private async Task SaveSectionAsync()
    {
        var currentSectionId = sectionForm.SectionId;
        var isNewSection = currentSectionId is null;
        var result = await MenuManagementService.SaveSectionAsync(
            new SaveMenuSectionRequest(
                sectionForm.SectionId,
                sectionForm.Name,
                sectionForm.Callout,
                sectionForm.Family,
                sectionForm.ParentSectionId,
                GetSelectedSectionTabs().ToArray(),
                sectionForm.SortOrder,
                sectionForm.IsVisibleToGuests,
                sectionForm.IsArchived));

        if (!await HandleOperationResultAsync(result, currentSectionId is null ? "Section created." : "Section updated."))
        {
            return;
        }

        var savedSectionId = result.EntityId ?? currentSectionId;
        if (savedSectionId is Guid trackedSectionId && sectionForm.IsArchived)
        {
            sessionVisibleEmptySectionIds.Remove(trackedSectionId);
        }
        else if (isNewSection && savedSectionId is Guid createdSectionId && !Items.Any(item => item.SectionAssignments.Any(assignment => assignment.SectionId == createdSectionId)))
        {
            sessionVisibleEmptySectionIds.Add(createdSectionId);
        }

        TrySelectSectionById(savedSectionId);
    }

    private async Task SaveItemAsync()
    {
        try
        {
            ClearDuplicateItemPrompt();

            var sectionIds = itemForm.SelectedSectionIds
                .Where(sectionId => Sections.Any(section => section.SectionId == sectionId))
                .Distinct()
                .ToArray();
            if (sectionIds.Length == 0)
            {
                statusMessage = "Error: Choose at least one section before saving the menu item.";
                return;
            }

            if (!TryParseOptionalDate(itemForm.OfferStartsOnText, "Offer start date", out var offerStartsOn, out var dateError)
                || !TryParseOptionalDate(itemForm.OfferEndsOnText, "Offer end date", out var offerEndsOn, out dateError))
            {
                statusMessage = $"Error: {dateError}";
                return;
            }

            if (!TryBuildSeasonalWindow(
                    out var seasonStartMonth,
                    out var seasonStartDay,
                    out var seasonEndMonth,
                    out var seasonEndDay,
                    out var seasonalError))
            {
                statusMessage = $"Error: {seasonalError}";
                return;
            }

            if (!TryBuildPriceVariants(itemForm.PriceVariants, out var priceVariants, out var priceError))
            {
                statusMessage = $"Error: {priceError}";
                return;
            }

            SaveMenuItemSpecialRequest? specialRequest = null;
            if (itemForm.IsSpecial)
            {
                if (!TryBuildSpecialRequest(out specialRequest, out var specialError))
                {
                    statusMessage = $"Error: {specialError}";
                    return;
                }
            }

            var currentItemId = itemForm.ItemId;
            var result = await MenuManagementService.SaveItemAsync(
                new SaveMenuItemRequest(
                    itemForm.ItemId,
                    itemForm.Name,
                    itemForm.Description,
                    NormalizeStoredImagePath(itemForm.ImagePath),
                    itemForm.SortOrder,
                    itemForm.IsVisibleToGuests,
                    itemForm.IsArchived,
                    offerStartsOn,
                    offerEndsOn,
                    itemForm.IsSeasonal,
                    seasonStartMonth,
                    seasonStartDay,
                    seasonEndMonth,
                    seasonEndDay,
                    priceVariants,
                    sectionIds
                        .Select(sectionId => new SaveMenuItemSectionAssignmentRequest(
                            sectionId,
                            itemForm.SectionSortOrders.GetValueOrDefault(sectionId, itemForm.SortOrder)))
                        .ToArray(),
                    itemForm.UseSectionVisibility,
                    GetSelectedMenuTabs().ToArray(),
                    specialRequest));

            if (!await HandleOperationResultAsync(result, currentItemId is null ? "Menu item created." : "Menu item updated."))
            {
                return;
            }

            foreach (var sectionId in sectionIds)
            {
                sessionVisibleEmptySectionIds.Remove(sectionId);
            }

            TrySelectItemById(result.EntityId ?? currentItemId);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Menu item save failed for item {ItemId}", itemForm.ItemId);
            statusMessage = "Error: We couldn't save the menu item. Refresh the page and try again.";
        }
    }

    private bool TryBuildPriceVariants(
        IReadOnlyList<MenuItemPriceVariantFormModel> variants,
        out IReadOnlyList<SaveMenuItemPriceVariantRequest> requests,
        out string? error)
    {
        requests = Array.Empty<SaveMenuItemPriceVariantRequest>();
        error = null;

        var activeVariants = variants
            .Select((variant, index) => new { Variant = variant, Index = index })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Variant.Label) || !string.IsNullOrWhiteSpace(entry.Variant.AmountText))
            .ToArray();

        if (activeVariants.Length == 0)
        {
            error = "Add at least one price variant before saving the menu item.";
            return false;
        }

        List<SaveMenuItemPriceVariantRequest> builtVariants = [];
        foreach (var entry in activeVariants)
        {
            var label = entry.Variant.Label.Trim();
            if (string.IsNullOrWhiteSpace(label))
            {
                error = "Each price variant needs a label.";
                return false;
            }

            if (!decimal.TryParse(entry.Variant.AmountText, NumberStyles.Number, InvariantCulture, out var amount))
            {
                error = $"Enter a valid number for the '{label}' price.";
                return false;
            }

            builtVariants.Add(new SaveMenuItemPriceVariantRequest(entry.Variant.PriceVariantId, label, amount, entry.Index + 1));
        }

        requests = builtVariants;
        return true;
    }

    private bool TryBuildSeasonalWindow(
        out int? seasonStartMonth,
        out int? seasonStartDay,
        out int? seasonEndMonth,
        out int? seasonEndDay,
        out string? error)
    {
        seasonStartMonth = null;
        seasonStartDay = null;
        seasonEndMonth = null;
        seasonEndDay = null;
        error = null;

        if (!itemForm.UseRecurringSeasonWindow)
        {
            return true;
        }

        if (itemForm.SeasonStartMonth is null || itemForm.SeasonEndMonth is null)
        {
            error = "Recurring seasonal availability needs both a start month and an end month.";
            return false;
        }

        if (itemForm.SeasonStartDay is < 1 or > 31 || itemForm.SeasonEndDay is < 1 or > 31)
        {
            error = "Seasonal day values must be between 1 and 31.";
            return false;
        }

        seasonStartMonth = itemForm.SeasonStartMonth;
        seasonStartDay = itemForm.SeasonStartDay;
        seasonEndMonth = itemForm.SeasonEndMonth;
        seasonEndDay = itemForm.SeasonEndDay;
        return true;
    }

    private bool TryBuildSpecialRequest(out SaveMenuItemSpecialRequest? request, out string? error)
    {
        request = null;

        if (!TryParseOptionalTime(itemForm.SpecialStartsAtText, "Special start time", out var startsAt, out error)
            || !TryParseOptionalTime(itemForm.SpecialEndsAtText, "Special end time", out var endsAt, out error))
        {
            return false;
        }

        DateOnly? startDate = null;
        DateOnly? endDate = null;
        if (itemForm.SpecialScheduleKind == MenuItemSpecialScheduleKind.Dated)
        {
            if (!TryParseRequiredDate(itemForm.SpecialStartDateText, "Special start date", out startDate, out error))
            {
                return false;
            }

            if (!TryParseOptionalDate(itemForm.SpecialEndDateText, "Special end date", out endDate, out error))
            {
                return false;
            }
        }

        request = new SaveMenuItemSpecialRequest(
            itemForm.SpecialScheduleKind,
            itemForm.SpecialScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
                ? itemForm.SelectedSpecialDays.OrderBy(day => Array.IndexOf(OrderedDays, day)).ToArray()
                : Array.Empty<DayOfWeek>(),
            startDate,
            endDate,
            startsAt,
            endsAt,
            itemForm.SpecialClosesNextDay,
            NormalizeOptionalValue(itemForm.SpecialCallout));

        return true;
    }

    private async Task SaveServiceHoursAsync()
    {
        if (!CanSaveHours)
        {
            if (TryValidateHoursForm(serviceWindowForm, out var validationError))
            {
                statusMessage = "Error: Make a change before saving service hours.";
            }
            else
            {
                statusMessage = $"Error: {validationError}";
            }

            return;
        }

        var dayRequests = new List<SaveMenuServiceWindowDayRequest>(serviceWindowForm.Days.Count);

        foreach (var day in serviceWindowForm.Days.OrderBy(item => Array.IndexOf(OrderedDays, item.DayOfWeek)))
        {
            if (!day.IsAvailable)
            {
                dayRequests.Add(new SaveMenuServiceWindowDayRequest(day.DayOfWeek, false, null, null, false));
                continue;
            }

            if (!TryParseRequiredTime(day.OpensAtText, $"{GetDayLabel(day.DayOfWeek)} opening time", out var opensAt, out var timeError)
                || !TryParseRequiredTime(day.ClosesAtText, $"{GetDayLabel(day.DayOfWeek)} closing time", out var closesAt, out timeError))
            {
                statusMessage = $"Error: {timeError}";
                return;
            }

            dayRequests.Add(new SaveMenuServiceWindowDayRequest(day.DayOfWeek, true, opensAt, closesAt, day.ClosesNextDay));
        }

        var result = await MenuManagementService.SaveServiceWindowsAsync(new SaveMenuServiceWindowRequest(serviceWindowForm.Tab, dayRequests));
        if (await HandleOperationResultAsync(result, $"{GetTabLabel(serviceWindowForm.Tab)} service hours updated."))
        {
            ResetHoursForm(serviceWindowForm.Tab);
        }
    }

    private async Task ArchiveSectionAsync(Guid sectionId)
    {
        var result = await MenuManagementService.ArchiveSectionAsync(sectionId);
        if (await HandleOperationResultAsync(result, "Section archived."))
        {
            sessionVisibleEmptySectionIds.Remove(sectionId);
            ReloadSelectedDetailOrEnsureSelection();
        }
    }

    private async Task ArchiveItemAsync(Guid itemId)
    {
        var result = await MenuManagementService.ArchiveItemAsync(itemId);
        if (await HandleOperationResultAsync(result, "Menu item archived."))
        {
            ReloadSelectedDetailOrEnsureSelection();
        }
    }

    private async Task ToggleSectionDeleteAsync(Guid sectionId)
    {
        if (pendingSectionDeleteId == sectionId)
        {
            pendingSectionDeleteId = null;
            var result = await MenuManagementService.DeleteSectionAsync(sectionId);
            if (await HandleOperationResultAsync(result, "Section deleted."))
            {
                sessionVisibleEmptySectionIds.Remove(sectionId);
                if (sectionForm.SectionId == sectionId)
                {
                    detailKind = MenuAdminDetailKind.None;
                    selectedBrowserId = null;
                }

                EnsureSelectionForCurrentTab();
            }

            return;
        }

        pendingSectionDeleteId = sectionId;
        pendingItemDeleteId = null;
    }

    private async Task ToggleItemDeleteAsync(Guid itemId)
    {
        if (pendingItemDeleteId == itemId)
        {
            pendingItemDeleteId = null;
            var result = await MenuManagementService.DeleteItemAsync(itemId);
            if (await HandleOperationResultAsync(result, "Menu item deleted."))
            {
                if (itemForm.ItemId == itemId)
                {
                    detailKind = MenuAdminDetailKind.None;
                    selectedBrowserId = null;
                }

                EnsureSelectionForCurrentTab();
            }

            return;
        }

        pendingItemDeleteId = itemId;
        pendingSectionDeleteId = null;
    }

    private async Task HandleSectionFamilyChangedAsync(ChangeEventArgs args)
    {
        if (args.Value is string familyValue && Enum.TryParse<MenuFamily>(familyValue, out var family))
        {
            sectionForm.Family = family;
            NormalizeSectionFormForSelectedFamily();
            ClearSectionValidationMessage();
            if (sectionForm.ParentSectionId is { } parentSectionId
                && ParentSectionOptions.All(section => section.SectionId != parentSectionId))
            {
                sectionForm.ParentSectionId = null;
            }
        }

        await Task.CompletedTask;
    }

    private async Task HandleItemSectionSelectionChangedAsync(Guid sectionId, ChangeEventArgs args)
    {
        var isSelected = args.Value is bool selected && selected;
        if (isSelected)
        {
            itemForm.SelectedSectionIds.Add(sectionId);
            itemForm.SectionSortOrders[sectionId] = itemForm.ItemId is null
                ? GetNextItemSortOrder(sectionId, itemForm.IsSpecial)
                : itemForm.SectionSortOrders.GetValueOrDefault(sectionId, GetNextItemSortOrder(sectionId, itemForm.IsSpecial));
            itemForm.ActiveSectionId ??= sectionId;
        }
        else
        {
            itemForm.SelectedSectionIds.Remove(sectionId);
            itemForm.SectionSortOrders.Remove(sectionId);
            if (itemForm.ActiveSectionId == sectionId)
            {
                itemForm.ActiveSectionId = itemForm.SelectedSectionIds.OrderBy(id => id).FirstOrDefault();
            }
        }

        ClearDuplicateItemPrompt();
        NormalizeItemFormForSelectedSections();
        itemForm.SortOrder = GetCurrentItemSortOrder();

        await Task.CompletedTask;
    }

    private async Task HandleItemSortOrderChangedAsync()
    {
        var activeSectionId = GetCurrentItemSortOrderContextSectionId();
        if (activeSectionId is Guid sectionId)
        {
            itemForm.SectionSortOrders[sectionId] = Math.Max(1, itemForm.SortOrder);
        }

        await Task.CompletedTask;
    }

    private async Task HandleSpecialToggleChangedAsync(ChangeEventArgs args)
    {
        itemForm.IsSpecial = args.Value is bool selected && selected;
        ClearDuplicateItemPrompt();

        if (itemForm.ItemId is null)
        {
            var activeSectionId = GetCurrentItemSortOrderContextSectionId();
            if (activeSectionId is Guid sectionId)
            {
                itemForm.SectionSortOrders[sectionId] = GetNextItemSortOrder(sectionId, itemForm.IsSpecial);
            }

            itemForm.SortOrder = GetCurrentItemSortOrder();
        }

        await Task.CompletedTask;
    }

    private Task ToggleSectionTabAsync(MenuTab tab)
    {
        switch (tab)
        {
            case MenuTab.Breakfast:
                sectionForm.ShowBreakfast = !sectionForm.ShowBreakfast;
                break;
            case MenuTab.Lunch:
                sectionForm.ShowLunch = !sectionForm.ShowLunch;
                break;
            case MenuTab.Dinner:
                sectionForm.ShowDinner = !sectionForm.ShowDinner;
                break;
            case MenuTab.Drinks:
                sectionForm.ShowDrinks = !sectionForm.ShowDrinks;
                break;
        }

        NormalizeSectionFormForSelectedFamily();
        ClearSectionValidationMessage();
        return Task.CompletedTask;
    }

    private Task ToggleItemMenuTabAsync(MenuTab tab)
    {
        if (itemForm.UseSectionVisibility)
        {
            return Task.CompletedTask;
        }

        switch (tab)
        {
            case MenuTab.Breakfast:
                itemForm.ShowBreakfast = !itemForm.ShowBreakfast;
                break;
            case MenuTab.Lunch:
                itemForm.ShowLunch = !itemForm.ShowLunch;
                break;
            case MenuTab.Dinner:
                itemForm.ShowDinner = !itemForm.ShowDinner;
                break;
        }

        NormalizeItemFormForSelectedSections();
        return Task.CompletedTask;
    }

    private Task SetItemUseSectionVisibilityAsync(bool useSectionVisibility)
    {
        itemForm.UseSectionVisibility = useSectionVisibility;
        NormalizeItemFormForSelectedSections();
        return Task.CompletedTask;
    }

    private Task SetRecurringSeasonWindowEnabledAsync(bool isEnabled)
    {
        itemForm.UseRecurringSeasonWindow = isEnabled;
        if (!isEnabled)
        {
            itemForm.SeasonStartMonth = null;
            itemForm.SeasonStartDay = null;
            itemForm.SeasonEndMonth = null;
            itemForm.SeasonEndDay = null;
        }

        return Task.CompletedTask;
    }

    private Task ToggleSpecialDayAsync(DayOfWeek dayOfWeek)
    {
        if (!itemForm.SelectedSpecialDays.Add(dayOfWeek))
        {
            itemForm.SelectedSpecialDays.Remove(dayOfWeek);
        }

        return Task.CompletedTask;
    }

    private async Task SelectHoursTabAsync(MenuTab tab)
    {
        ResetHoursForm(tab);
        await Task.CompletedTask;
    }

    private void AddPriceVariant()
    {
        itemForm.PriceVariants.Add(new MenuItemPriceVariantFormModel
        {
            SortOrder = itemForm.PriceVariants.Count + 1
        });
    }

    private void RemovePriceVariant(MenuItemPriceVariantFormModel variant)
    {
        if (itemForm.PriceVariants.Count == 1)
        {
            return;
        }

        itemForm.PriceVariants.Remove(variant);

        for (var index = 0; index < itemForm.PriceVariants.Count; index++)
        {
            itemForm.PriceVariants[index].SortOrder = index + 1;
        }
    }

    private void NormalizeSectionFormForSelectedFamily()
    {
        if (sectionForm.Family == MenuFamily.Drink)
        {
            sectionForm.ShowBreakfast = false;
            sectionForm.ShowLunch = false;
            sectionForm.ShowDinner = false;
            sectionForm.ShowDrinks = true;
            return;
        }

        sectionForm.ShowDrinks = false;
        if (!sectionForm.ShowBreakfast && !sectionForm.ShowLunch && !sectionForm.ShowDinner)
        {
            sectionForm.ShowLunch = true;
            sectionForm.ShowDinner = true;
        }
    }

    private async Task HandleItemImageSelectedAsync(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file is null)
        {
            return;
        }

        ClearItemImageFeedback();
        isItemImageUploading = true;

        try
        {
            await using var stream = file.OpenReadStream(MenuItemImageStorageDefaults.MaxRawUploadBytes);
            itemForm.ImagePath = await MenuItemImageStorage.SaveImageAsync(
                stream,
                file.Name,
                file.ContentType,
                file.Size);
            itemImageUploadStatusMessage = "Image uploaded. Save the item to keep this image on the menu record.";
        }
        catch (MenuItemImageUploadException exception)
        {
            itemImageUploadErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Menu item image upload failed for item {ItemId}", itemForm.ItemId);
            itemImageUploadErrorMessage = "We couldn't finish uploading that image. Try again with a different file.";
        }
        finally
        {
            isItemImageUploading = false;
        }
    }

    private void ClearSectionValidationMessage()
    {
        if (detailKind == MenuAdminDetailKind.Section
            && (string.Equals(statusMessage, "Error: Drink sections must appear on Drinks.", StringComparison.Ordinal)
                || string.Equals(statusMessage, "Error: Food sections must appear on at least one of Breakfast, Lunch, or Dinner.", StringComparison.Ordinal)))
        {
            statusMessage = null;
        }
    }

    private void NormalizeItemFormForSelectedSections()
    {
        itemForm.ActiveSectionId = GetCurrentItemSortOrderContextSectionId();

        if (GetItemEditorFamily() == MenuFamily.Drink)
        {
            itemForm.ShowBreakfast = false;
            itemForm.ShowLunch = false;
            itemForm.ShowDinner = false;
            itemForm.UseSectionVisibility = true;
            return;
        }

        var allowedTabs = GetAllowedSectionTabsForItem().ToHashSet();
        if (!allowedTabs.Contains(MenuTab.Breakfast))
        {
            itemForm.ShowBreakfast = false;
        }

        if (!allowedTabs.Contains(MenuTab.Lunch))
        {
            itemForm.ShowLunch = false;
        }

        if (!allowedTabs.Contains(MenuTab.Dinner))
        {
            itemForm.ShowDinner = false;
        }

        if (!itemForm.UseSectionVisibility
            && !itemForm.ShowBreakfast
            && !itemForm.ShowLunch
            && !itemForm.ShowDinner)
        {
            if (allowedTabs.Contains(MenuTab.Lunch))
            {
                itemForm.ShowLunch = true;
            }
            else if (allowedTabs.Contains(MenuTab.Dinner))
            {
                itemForm.ShowDinner = true;
            }
            else if (allowedTabs.Contains(MenuTab.Breakfast))
            {
                itemForm.ShowBreakfast = true;
            }
        }
    }

    private async Task<bool> HandleOperationResultAsync(MenuOperationResult result, string successMessage)
    {
        if (!result.Succeeded)
        {
            statusMessage = $"Error: {string.Join(" ", result.Errors)}";
            return false;
        }

        await LoadAsync();
        ResetHoursForm(serviceWindowForm.Tab);
        statusMessage = successMessage;
        return true;
    }

    private Task HandleItemNameBlurAsync(FocusEventArgs _)
    {
        duplicateItemPrompt = null;

        var normalizedName = itemForm.Name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Task.CompletedTask;
        }

        var duplicateItem = Items
            .FirstOrDefault(item =>
                item.ItemId != itemForm.ItemId
                && string.Equals(MenuNameRules.NormalizeForLookup(item.Name), MenuNameRules.NormalizeForLookup(normalizedName), StringComparison.Ordinal));

        if (duplicateItem is not null)
        {
            duplicateItemPrompt = new MenuAdminDuplicateItemPromptViewModel(
                duplicateItem.ItemId,
                duplicateItem.Name,
                duplicateItem.Family,
                duplicateItem.IsArchived,
                duplicateItem.Special is not null);
        }

        return Task.CompletedTask;
    }

    private void DismissDuplicateItemPrompt() => ClearDuplicateItemPrompt();

    private void EditExistingDuplicateItem()
    {
        if (duplicateItemPrompt is null)
        {
            return;
        }

        var matchingItem = Items.SingleOrDefault(item => item.ItemId == duplicateItemPrompt.ItemId);
        if (matchingItem is null)
        {
            ClearDuplicateItemPrompt();
            return;
        }

        if (matchingItem.Family == MenuFamily.Drink)
        {
            selectedEditorTab = MenuEditorTab.Drinks;
            drinkContentFilter = MenuContentFilter.All;
            if (matchingItem.IsArchived)
            {
                drinkArchiveFilter = MenuArchiveFilter.Both;
            }
        }
        else
        {
            selectedEditorTab = MenuEditorTab.Food;
            selectedFoodFilter = null;
            foodContentFilter = MenuContentFilter.All;
            if (matchingItem.IsArchived)
            {
                foodArchiveFilter = MenuArchiveFilter.Both;
            }
        }

        SelectItem(matchingItem);
        UpdateLocation();
    }

    private void ReloadSelectedDetailOrEnsureSelection()
    {
        if (!TryReloadCurrentDetailFromData())
        {
            EnsureSelectionForCurrentTab();
        }
    }

    private bool TryReloadCurrentDetailFromData()
    {
        return detailKind switch
        {
            MenuAdminDetailKind.Section when sectionForm.SectionId is Guid sectionId => TrySelectSectionById(sectionId),
            MenuAdminDetailKind.Item when itemForm.ItemId is Guid itemId => TrySelectItemById(itemId),
            _ => false
        };
    }

    private bool TrySelectSectionById(Guid? sectionId)
    {
        if (sectionId is not Guid id)
        {
            return false;
        }

        var section = Sections.SingleOrDefault(item => item.SectionId == id);
        if (section is null)
        {
            return false;
        }

        SelectSection(section);
        return true;
    }

    private bool TrySelectItemById(Guid? itemId)
    {
        if (itemId is not Guid id)
        {
            return false;
        }

        var item = Items.SingleOrDefault(entry => entry.ItemId == id);
        if (item is null)
        {
            return false;
        }

        SelectItem(item, item.SectionAssignments.OrderBy(assignment => assignment.SortOrder).Select(assignment => (Guid?)assignment.SectionId).FirstOrDefault());
        return true;
    }

    private void ClearPendingDeletes()
    {
        pendingSectionDeleteId = null;
        pendingItemDeleteId = null;
    }

    private void ClearDuplicateItemPrompt() => duplicateItemPrompt = null;

    private void ClearItemImageFeedback()
    {
        itemImageUploadStatusMessage = null;
        itemImageUploadErrorMessage = null;
        isItemImageUploading = false;
    }

    private IReadOnlyList<MenuAdminBrowserSectionViewModel> BuildBrowserSections(
        MenuFamily family,
        MenuTab? foodFilter,
        MenuArchiveFilter archiveFilter,
        MenuContentFilter contentFilter)
    {
        List<MenuAdminBrowserSectionViewModel> browserSections = [];

        foreach (var section in Sections
                     .Where(section => section.Family == family)
                     .OrderBy(section => section.SortOrder)
                     .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase))
        {
            var sectionMatchesMenu = MatchesSectionFilter(section, foodFilter);
            var sectionItems = Items
                .Where(item => item.SectionAssignments.Any(assignment => assignment.SectionId == section.SectionId))
                .Where(item => item.Family == family)
                .ToArray();
            var itemEntries = BuildBrowserItemEntries(section, sectionMatchesMenu, family, foodFilter, archiveFilter, contentFilter);
            var childSections = BuildBrowserChildSections(section.SectionId, family, foodFilter, archiveFilter, contentFilter);

            var keepEmptySectionVisible = sectionItems.Length == 0
                && (detailKind == MenuAdminDetailKind.Section && sectionForm.SectionId == section.SectionId
                    || sessionVisibleEmptySectionIds.Contains(section.SectionId));
            var sectionMatches = MatchesArchiveFilter(section.IsArchived, archiveFilter);
            if (!ShouldIncludeBrowserSection(
                    sectionMatchesMenu,
                    sectionMatches,
                    keepEmptySectionVisible,
                    sectionItems.Length,
                    childSections.Length,
                    itemEntries.Length,
                    contentFilter))
            {
                continue;
            }

            browserSections.Add(new MenuAdminBrowserSectionViewModel(
                section,
                IsContextMuted: !sectionMatches && archiveFilter != MenuArchiveFilter.Both && (itemEntries.Length > 0 || childSections.Length > 0),
                ChildSections: childSections,
                Entries: BuildBrowserSectionEntries(section.SectionId, itemEntries, childSections),
                Items: itemEntries));
        }

        return browserSections;
    }

    private MenuAdminBrowserSectionEntryViewModel[] BuildBrowserSectionEntries(
        Guid parentSectionId,
        IReadOnlyList<MenuAdminBrowserItemViewModel> itemEntries,
        IReadOnlyList<MenuAdminBrowserChildSectionViewModel> childSections) =>
        childSections
            .Select(childSection => new MenuAdminBrowserSectionEntryViewModel(childSection.Section.SortOrder, null, childSection))
            .Concat(itemEntries.Select(itemEntry => new MenuAdminBrowserSectionEntryViewModel(GetSectionAssignmentSortOrder(itemEntry.Item, parentSectionId), itemEntry, null)))
            .OrderBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.IsItem ? 1 : 0)
            .ThenBy(entry => GetBrowserSectionEntryTitle(entry), StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private MenuAdminBrowserItemViewModel[] BuildBrowserItemEntries(
        MenuSectionAdminView section,
        bool sectionMatchesMenu,
        MenuFamily family,
        MenuTab? foodFilter,
        MenuArchiveFilter archiveFilter,
        MenuContentFilter contentFilter) =>
        Items
            .Where(item => item.SectionAssignments.Any(assignment => assignment.SectionId == section.SectionId))
            .Where(item => item.Family == family)
            .Where(_ => sectionMatchesMenu)
            .Where(item => MatchesItemFilter(item, foodFilter))
            .Where(item => MatchesArchiveFilter(item.IsArchived, archiveFilter))
            .Where(item => MatchesContentFilter(item, contentFilter))
            .OrderByDescending(item => item.Special is not null)
            .ThenBy(item => GetSectionAssignmentSortOrder(item, section.SectionId))
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new MenuAdminBrowserItemViewModel(item, section.SectionId, false))
            .ToArray();

    private MenuAdminBrowserChildSectionViewModel[] BuildBrowserChildSections(
        Guid parentSectionId,
        MenuFamily family,
        MenuTab? foodFilter,
        MenuArchiveFilter archiveFilter,
        MenuContentFilter contentFilter)
    {
        List<MenuAdminBrowserChildSectionViewModel> childSections = [];

        foreach (var childSection in GetOrderedSections(family, parentSectionId))
        {
            var childMatchesMenu = MatchesSectionFilter(childSection, foodFilter);
            var childItems = Items
                .Where(item => item.SectionAssignments.Any(assignment => assignment.SectionId == childSection.SectionId))
                .Where(item => item.Family == family)
                .ToArray();
            var childItemEntries = BuildBrowserItemEntries(childSection, childMatchesMenu, family, foodFilter, archiveFilter, contentFilter);
            var keepEmptyChildVisible = childItems.Length == 0
                && (detailKind == MenuAdminDetailKind.Section && sectionForm.SectionId == childSection.SectionId
                    || sessionVisibleEmptySectionIds.Contains(childSection.SectionId));
            var childMatchesArchive = MatchesArchiveFilter(childSection.IsArchived, archiveFilter);

            if (!ShouldIncludeBrowserSection(
                    childMatchesMenu,
                    childMatchesArchive,
                    keepEmptyChildVisible,
                    childItems.Length,
                    0,
                    childItemEntries.Length,
                    contentFilter))
            {
                continue;
            }

            childSections.Add(new MenuAdminBrowserChildSectionViewModel(
                childSection,
                IsContextMuted: !childMatchesArchive && archiveFilter != MenuArchiveFilter.Both && childItemEntries.Length > 0));
        }

        return childSections.ToArray();
    }

    private static bool ShouldIncludeBrowserSection(
        bool sectionMatchesMenu,
        bool sectionMatchesArchive,
        bool keepEmptySectionVisible,
        int sectionItemCount,
        int visibleChildSectionCount,
        int visibleItemCount,
        MenuContentFilter contentFilter)
    {
        if (!sectionMatchesMenu && !keepEmptySectionVisible && visibleItemCount == 0 && visibleChildSectionCount == 0)
        {
            return false;
        }

        if (!keepEmptySectionVisible && !sectionMatchesArchive && visibleItemCount == 0 && visibleChildSectionCount == 0)
        {
            return false;
        }

        if (!keepEmptySectionVisible && sectionItemCount == 0 && visibleChildSectionCount == 0)
        {
            return false;
        }

        if (!keepEmptySectionVisible && contentFilter != MenuContentFilter.All && visibleItemCount == 0 && visibleChildSectionCount == 0)
        {
            return false;
        }

        return true;
    }

    private static bool MatchesArchiveFilter(bool isArchived, MenuArchiveFilter archiveFilter) =>
        archiveFilter switch
        {
            MenuArchiveFilter.Active => !isArchived,
            MenuArchiveFilter.Archived => isArchived,
            _ => true
        };

    private static bool MatchesSectionFilter(MenuSectionAdminView section, MenuTab? foodFilter) =>
        section.Family == MenuFamily.Drink || foodFilter is null || section.MenuTabs.Contains(foodFilter.Value);

    private static bool MatchesItemFilter(MenuItemAdminView item, MenuTab? foodFilter) =>
        item.Family == MenuFamily.Drink
        || foodFilter is null
        || item.UsesSectionVisibility
        || item.MenuTabs.Contains(foodFilter.Value);

    private static bool MatchesContentFilter(MenuItemAdminView item, MenuContentFilter filter) =>
        filter switch
        {
            MenuContentFilter.Standard => item.Special is null,
            MenuContentFilter.Specials => item.Special is not null,
            _ => true
        };

    private void BeginSectionDrag(MenuSectionAdminView section, Guid? contextSectionId = null) =>
        dragState = new MenuBrowserDragState(MenuAdminDetailKind.Section, section.SectionId, contextSectionId, section.Family, false);

    private void BeginItemDrag(MenuAdminBrowserItemViewModel itemEntry) =>
        dragState = new MenuBrowserDragState(MenuAdminDetailKind.Item, itemEntry.Item.ItemId, itemEntry.SectionId, itemEntry.Item.Family, itemEntry.Item.Special is not null);

    private async Task DropSectionAsync(MenuSectionAdminView targetSection)
    {
        if (dragState is not { Kind: MenuAdminDetailKind.Section } state
            || state.RecordId == targetSection.SectionId
            || state.Family != targetSection.Family)
        {
            return;
        }

        var sourceSection = Sections.SingleOrDefault(section => section.SectionId == state.RecordId && section.Family == state.Family);
        if (sourceSection is null || sourceSection.ParentSectionId != targetSection.ParentSectionId)
        {
            dragState = null;
            return;
        }

        dragState = null;
        await ReorderSectionsAsync(state.RecordId, targetSection.SectionId, targetSection.Family, targetSection.ParentSectionId);
    }

    private async Task DropSectionContentEntryAsync(Guid parentSectionId, MenuAdminBrowserSectionEntryViewModel targetEntry)
    {
        if (dragState is not { SectionId: { } contextSectionId } state
            || contextSectionId != parentSectionId)
        {
            return;
        }

        if (state.Kind == MenuAdminDetailKind.Section)
        {
            var sourceSection = Sections.SingleOrDefault(section => section.SectionId == state.RecordId && section.Family == state.Family);
            if (sourceSection?.ParentSectionId != parentSectionId)
            {
                dragState = null;
                return;
            }
        }
        else if (state.Kind == MenuAdminDetailKind.Item)
        {
            var sourceItem = Items.SingleOrDefault(item => item.ItemId == state.RecordId && item.Family == state.Family);
            if (sourceItem is null || !sourceItem.SectionAssignments.Any(assignment => assignment.SectionId == parentSectionId))
            {
                dragState = null;
                return;
            }
        }
        else
        {
            return;
        }

        dragState = null;
        await ReorderSectionContentAsync(state.RecordId, GetBrowserSectionEntryRecordId(targetEntry), state.Family, parentSectionId);
    }

    private async Task DropItemAsync(MenuAdminBrowserItemViewModel targetItem)
    {
        if (dragState is not { Kind: MenuAdminDetailKind.Item, SectionId: { } sectionId } state
            || state.RecordId == targetItem.Item.ItemId
            || sectionId != targetItem.SectionId)
        {
            return;
        }

        if (SectionHasChildSections(targetItem.SectionId, targetItem.Item.Family))
        {
            dragState = null;
            await ReorderSectionContentAsync(state.RecordId, targetItem.Item.ItemId, targetItem.Item.Family, targetItem.SectionId);
            return;
        }

        if (state.IsSpecialGroup != (targetItem.Item.Special is not null))
        {
            statusMessage = "Error: Special items can only be reordered with other special items, and standard items can only be reordered with other standard items.";
            dragState = null;
            return;
        }

        dragState = null;
        await ReorderItemsAsync(state.RecordId, targetItem.Item.ItemId, targetItem.SectionId, targetItem.Item.Special is not null);
    }

    private async Task MoveSectionAsync(MenuSectionAdminView section, int direction)
    {
        var siblings = GetOrderedSections(section.Family, section.ParentSectionId);
        var currentIndex = Array.FindIndex(siblings, sibling => sibling.SectionId == section.SectionId);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= siblings.Length)
        {
            return;
        }

        await ReorderSectionsAsync(section.SectionId, siblings[targetIndex].SectionId, section.Family, section.ParentSectionId);
    }

    private async Task MoveSectionContentEntryAsync(Guid parentSectionId, MenuAdminBrowserSectionEntryViewModel entry, int direction)
    {
        var siblings = GetOrderedSectionContentEntries(parentSectionId, GetBrowserSectionEntryFamily(entry));
        var currentIndex = Array.FindIndex(siblings, sibling => GetBrowserSectionEntryRecordId(sibling) == GetBrowserSectionEntryRecordId(entry));
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= siblings.Length)
        {
            return;
        }

        await ReorderSectionContentAsync(
            GetBrowserSectionEntryRecordId(entry),
            GetBrowserSectionEntryRecordId(siblings[targetIndex]),
            GetBrowserSectionEntryFamily(entry),
            parentSectionId);
    }

    private async Task MoveItemAsync(MenuAdminBrowserItemViewModel itemEntry, int direction)
    {
        if (SectionHasChildSections(itemEntry.SectionId, itemEntry.Item.Family))
        {
            var mixedSiblings = GetOrderedSectionContentEntries(itemEntry.SectionId, itemEntry.Item.Family);
            var mixedCurrentIndex = Array.FindIndex(mixedSiblings, sibling => sibling.ItemEntry?.Item.ItemId == itemEntry.Item.ItemId);
            var mixedTargetIndex = mixedCurrentIndex + direction;

            if (mixedCurrentIndex < 0 || mixedTargetIndex < 0 || mixedTargetIndex >= mixedSiblings.Length)
            {
                return;
            }

            await ReorderSectionContentAsync(itemEntry.Item.ItemId, GetBrowserSectionEntryRecordId(mixedSiblings[mixedTargetIndex]), itemEntry.Item.Family, itemEntry.SectionId);
            return;
        }

        var siblings = GetOrderedItemGroup(itemEntry.SectionId, itemEntry.Item.Special is not null);
        var currentIndex = Array.FindIndex(siblings, sibling => sibling.ItemId == itemEntry.Item.ItemId);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= siblings.Length)
        {
            return;
        }

        await ReorderItemsAsync(itemEntry.Item.ItemId, siblings[targetIndex].ItemId, itemEntry.SectionId, itemEntry.Item.Special is not null);
    }

    private async Task ReorderSectionsAsync(Guid sourceSectionId, Guid targetSectionId, MenuFamily family, Guid? parentSectionId)
    {
        var orderedSections = GetOrderedSections(family, parentSectionId).ToList();
        if (!MoveIntoTargetSlot(orderedSections, section => section.SectionId, sourceSectionId, targetSectionId))
        {
            return;
        }

        if (await PersistSectionOrderAsync(orderedSections, "Section order updated."))
        {
            ReloadSelectedDetailOrEnsureSelection();
        }
    }

    private async Task ReorderItemsAsync(Guid sourceItemId, Guid targetItemId, Guid sectionId, bool isSpecialGroup)
    {
        var orderedItems = GetOrderedItemGroup(sectionId, isSpecialGroup).ToList();
        if (!MoveIntoTargetSlot(orderedItems, item => item.ItemId, sourceItemId, targetItemId))
        {
            return;
        }

        if (await PersistItemOrderAsync(orderedItems, sectionId, "Menu item order updated."))
        {
            ReloadSelectedDetailOrEnsureSelection();
        }
    }

    private async Task ReorderSectionContentAsync(Guid sourceRecordId, Guid targetRecordId, MenuFamily family, Guid parentSectionId)
    {
        var orderedEntries = GetOrderedSectionContentEntries(parentSectionId, family).ToList();
        if (!MoveIntoTargetSlot(orderedEntries, GetBrowserSectionEntryRecordId, sourceRecordId, targetRecordId))
        {
            return;
        }

        if (await PersistSectionContentOrderAsync(orderedEntries, parentSectionId, "Section content order updated."))
        {
            ReloadSelectedDetailOrEnsureSelection();
        }
    }

    private MenuSectionAdminView[] GetOrderedSections(MenuFamily family, Guid? parentSectionId) =>
        Sections
            .Where(section => section.Family == family && section.ParentSectionId == parentSectionId)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private MenuItemAdminView[] GetOrderedItemGroup(Guid sectionId, bool isSpecialGroup) =>
        Items
            .Where(item => item.SectionAssignments.Any(assignment => assignment.SectionId == sectionId) && (item.Special is not null) == isSpecialGroup)
            .OrderBy(item => GetSectionAssignmentSortOrder(item, sectionId))
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private MenuAdminBrowserSectionEntryViewModel[] GetOrderedSectionContentEntries(Guid parentSectionId, MenuFamily family)
    {
        var itemEntries = Items
            .Where(item => item.Family == family && item.SectionAssignments.Any(assignment => assignment.SectionId == parentSectionId))
            .Select(item => new MenuAdminBrowserItemViewModel(item, parentSectionId, false))
            .ToArray();
        var childSections = Sections
            .Where(section => section.Family == family && section.ParentSectionId == parentSectionId)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .Select(section => new MenuAdminBrowserChildSectionViewModel(section, false))
            .ToArray();

        return BuildBrowserSectionEntries(parentSectionId, itemEntries, childSections);
    }

    private async Task<bool> PersistSectionOrderAsync(IReadOnlyList<MenuSectionAdminView> orderedSections, string successMessage)
    {
        List<SaveMenuSortOrderRequest> updates = [];
        for (var index = 0; index < orderedSections.Count; index++)
        {
            var section = orderedSections[index];
            var desiredSortOrder = index + 1;
            if (section.SortOrder != desiredSortOrder)
            {
                updates.Add(new SaveMenuSortOrderRequest(section.SectionId, desiredSortOrder));
            }
        }

        if (updates.Count == 0)
        {
            return false;
        }

        var result = await MenuManagementService.ReorderSectionsAsync(updates);
        if (!result.Succeeded)
        {
            statusMessage = $"Error: {string.Join(" ", result.Errors)}";
            return false;
        }

        await LoadAsync();
        ResetHoursForm(serviceWindowForm.Tab);
        statusMessage = successMessage;
        return true;
    }

    private async Task<bool> PersistItemOrderAsync(IReadOnlyList<MenuItemAdminView> orderedItems, Guid sectionId, string successMessage)
    {
        List<SaveMenuSortOrderRequest> updates = [];
        for (var index = 0; index < orderedItems.Count; index++)
        {
            var item = orderedItems[index];
            var desiredSortOrder = index + 1;
            if (GetSectionAssignmentSortOrder(item, sectionId) != desiredSortOrder)
            {
                updates.Add(new SaveMenuSortOrderRequest(item.ItemId, desiredSortOrder, sectionId));
            }
        }

        if (updates.Count == 0)
        {
            return false;
        }

        var result = await MenuManagementService.ReorderItemsAsync(updates);
        if (!result.Succeeded)
        {
            statusMessage = $"Error: {string.Join(" ", result.Errors)}";
            return false;
        }

        await LoadAsync();
        ResetHoursForm(serviceWindowForm.Tab);
        statusMessage = successMessage;
        return true;
    }

    private async Task<bool> PersistSectionContentOrderAsync(
        IReadOnlyList<MenuAdminBrowserSectionEntryViewModel> orderedEntries,
        Guid parentSectionId,
        string successMessage)
    {
        List<SaveMenuSortOrderRequest> sectionUpdates = [];
        List<SaveMenuSortOrderRequest> itemUpdates = [];

        for (var index = 0; index < orderedEntries.Count; index++)
        {
            var entry = orderedEntries[index];
            var desiredSortOrder = index + 1;
            if (entry.ItemEntry is not null)
            {
                if (GetSectionAssignmentSortOrder(entry.ItemEntry.Item, parentSectionId) != desiredSortOrder)
                {
                    itemUpdates.Add(new SaveMenuSortOrderRequest(entry.ItemEntry.Item.ItemId, desiredSortOrder, parentSectionId));
                }
            }
            else if (entry.ChildSectionEntry is not null && entry.ChildSectionEntry.Section.SortOrder != desiredSortOrder)
            {
                sectionUpdates.Add(new SaveMenuSortOrderRequest(entry.ChildSectionEntry.Section.SectionId, desiredSortOrder));
            }
        }

        if (sectionUpdates.Count == 0 && itemUpdates.Count == 0)
        {
            return false;
        }

        if (sectionUpdates.Count > 0)
        {
            var sectionResult = await MenuManagementService.ReorderSectionsAsync(sectionUpdates);
            if (!sectionResult.Succeeded)
            {
                statusMessage = $"Error: {string.Join(" ", sectionResult.Errors)}";
                return false;
            }
        }

        if (itemUpdates.Count > 0)
        {
            var itemResult = await MenuManagementService.ReorderItemsAsync(itemUpdates);
            if (!itemResult.Succeeded)
            {
                statusMessage = $"Error: {string.Join(" ", itemResult.Errors)}";
                return false;
            }
        }

        await LoadAsync();
        ResetHoursForm(serviceWindowForm.Tab);
        statusMessage = successMessage;
        return true;
    }

    private bool CanMoveSection(MenuSectionAdminView section, int direction)
    {
        var siblings = GetOrderedSections(section.Family, section.ParentSectionId);
        var currentIndex = Array.FindIndex(siblings, sibling => sibling.SectionId == section.SectionId);
        var targetIndex = currentIndex + direction;
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < siblings.Length;
    }

    private bool CanMoveSectionContentEntry(Guid parentSectionId, MenuAdminBrowserSectionEntryViewModel entry, int direction)
    {
        var siblings = GetOrderedSectionContentEntries(parentSectionId, GetBrowserSectionEntryFamily(entry));
        var currentIndex = Array.FindIndex(siblings, sibling => GetBrowserSectionEntryRecordId(sibling) == GetBrowserSectionEntryRecordId(entry));
        var targetIndex = currentIndex + direction;
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < siblings.Length;
    }

    private bool CanMoveItem(MenuAdminBrowserItemViewModel itemEntry, int direction)
    {
        if (SectionHasChildSections(itemEntry.SectionId, itemEntry.Item.Family))
        {
            var mixedSiblings = GetOrderedSectionContentEntries(itemEntry.SectionId, itemEntry.Item.Family);
            var mixedCurrentIndex = Array.FindIndex(mixedSiblings, sibling => sibling.ItemEntry?.Item.ItemId == itemEntry.Item.ItemId);
            var mixedTargetIndex = mixedCurrentIndex + direction;
            return mixedCurrentIndex >= 0 && mixedTargetIndex >= 0 && mixedTargetIndex < mixedSiblings.Length;
        }

        var siblings = GetOrderedItemGroup(itemEntry.SectionId, itemEntry.Item.Special is not null);
        var currentIndex = Array.FindIndex(siblings, sibling => sibling.ItemId == itemEntry.Item.ItemId);
        var targetIndex = currentIndex + direction;
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < siblings.Length;
    }

    private bool SectionHasChildSections(Guid sectionId, MenuFamily family) =>
        Sections.Any(section => section.ParentSectionId == sectionId && section.Family == family);

    private static Guid GetBrowserSectionEntryRecordId(MenuAdminBrowserSectionEntryViewModel entry) =>
        entry.ItemEntry?.Item.ItemId
        ?? entry.ChildSectionEntry?.Section.SectionId
        ?? Guid.Empty;

    private static MenuFamily GetBrowserSectionEntryFamily(MenuAdminBrowserSectionEntryViewModel entry) =>
        entry.ItemEntry?.Item.Family
        ?? entry.ChildSectionEntry?.Section.Family
        ?? MenuFamily.Food;

    private static string GetBrowserSectionEntryTitle(MenuAdminBrowserSectionEntryViewModel entry) =>
        entry.ItemEntry?.Item.Name
        ?? entry.ChildSectionEntry?.Section.Name
        ?? string.Empty;

    private static bool MoveIntoTargetSlot<T>(List<T> records, Func<T, Guid> getId, Guid sourceId, Guid targetId)
    {
        var sourceIndex = records.FindIndex(record => getId(record) == sourceId);
        var targetIndex = records.FindIndex(record => getId(record) == targetId);

        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
        {
            return false;
        }

        var record = records[sourceIndex];
        records.RemoveAt(sourceIndex);
        records.Insert(targetIndex, record);
        return true;
    }

    private void ResetCurrentDetail()
    {
        switch (detailKind)
        {
            case MenuAdminDetailKind.Section when sectionForm.SectionId is Guid sectionId:
                TrySelectSectionById(sectionId);
                break;
            case MenuAdminDetailKind.Section:
                StartNewSection(CurrentContentFamily);
                break;
            case MenuAdminDetailKind.Item when itemForm.ItemId is Guid itemId:
                TrySelectItemById(itemId);
                break;
            case MenuAdminDetailKind.Item:
                StartNewItem(CurrentContentFamily, itemForm.IsSpecial);
                break;
        }
    }

    private MenuFamily GetSectionEditorFamily() => sectionForm.Family;

    private MenuFamily GetItemEditorFamily()
    {
        var selectedSectionId = GetCurrentItemSortOrderContextSectionId() ?? itemForm.SelectedSectionIds.OrderBy(id => id).FirstOrDefault();
        var selectedSection = selectedSectionId != Guid.Empty
            ? Sections.SingleOrDefault(section => section.SectionId == selectedSectionId)
            : null;

        return selectedSection?.Family ?? CurrentContentFamily;
    }

    private void CaptureSectionSnapshot() => sectionSnapshot = BuildSectionSnapshot(sectionForm);

    private void CaptureItemSnapshot() => itemSnapshot = BuildItemSnapshot(itemForm);

    private void CaptureHoursSnapshot() => hoursSnapshot = BuildHoursSnapshot(serviceWindowForm);

    private static string BuildSectionSnapshot(MenuSectionFormModel model) =>
        string.Join("|", model.SectionId, model.Name, model.Callout, model.Family, model.ParentSectionId, model.ShowBreakfast, model.ShowLunch, model.ShowDinner, model.ShowDrinks, model.SortOrder, model.IsVisibleToGuests, model.IsArchived);

    private static string BuildItemSnapshot(MenuItemFormModel model) =>
        string.Join("|",
            model.ItemId,
            string.Join(",", model.SelectedSectionIds.OrderBy(id => id)),
            string.Join(",", model.SectionSortOrders.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}:{entry.Value}")),
            model.ActiveSectionId,
            model.Name,
            model.Description,
            model.ImagePath,
            model.SortOrder,
            model.IsVisibleToGuests,
            model.IsArchived,
            model.OfferStartsOnText,
            model.OfferEndsOnText,
            model.IsSeasonal,
            model.UseRecurringSeasonWindow,
            model.SeasonStartMonth,
            model.SeasonStartDay,
            model.SeasonEndMonth,
            model.SeasonEndDay,
            model.UseSectionVisibility,
            model.ShowBreakfast,
            model.ShowLunch,
            model.ShowDinner,
            model.IsSpecial,
            model.SpecialScheduleKind,
            string.Join(",", model.SelectedSpecialDays.OrderBy(day => Array.IndexOf(OrderedDays, day))),
            model.SpecialStartDateText,
            model.SpecialEndDateText,
            model.SpecialStartsAtText,
            model.SpecialEndsAtText,
            model.SpecialClosesNextDay,
            model.SpecialCallout,
            string.Join(";", model.PriceVariants.Select(variant => $"{variant.PriceVariantId}~{variant.Label}~{variant.AmountText}~{variant.SortOrder}")));

    private static string BuildHoursSnapshot(MenuServiceWindowFormModel model) =>
        string.Join("|",
            model.Tab,
            string.Join(";", model.Days.Select(day => $"{day.DayOfWeek}~{day.IsAvailable}~{day.OpensAtText}~{day.ClosesAtText}~{day.ClosesNextDay}")));

    private static string? NormalizeStoredImagePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        var trimmed = imagePath.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            return trimmed;
        }

        return trimmed.StartsWith("/", StringComparison.Ordinal)
            ? trimmed
            : $"/{trimmed.TrimStart('/')}";
    }

    private static MenuItemFormModel CreateDefaultItemForm(bool isSpecial = false) => new()
    {
        IsSpecial = isSpecial
    };

    private Guid? GetCurrentItemSortOrderContextSectionId()
    {
        if (itemForm.ActiveSectionId is Guid activeSectionId && itemForm.SelectedSectionIds.Contains(activeSectionId))
        {
            return activeSectionId;
        }

        var firstSectionId = itemForm.SelectedSectionIds.OrderBy(id => id).FirstOrDefault();
        return firstSectionId == Guid.Empty ? null : firstSectionId;
    }

    private int GetCurrentItemSortOrder(int fallbackSortOrder = 1)
    {
        var activeSectionId = GetCurrentItemSortOrderContextSectionId();
        if (activeSectionId is Guid sectionId && itemForm.SectionSortOrders.TryGetValue(sectionId, out var sortOrder))
        {
            return sortOrder;
        }

        return fallbackSortOrder;
    }

    private static int GetSectionAssignmentSortOrder(MenuItemAdminView item, Guid sectionId) =>
        item.SectionAssignments
            .Where(assignment => assignment.SectionId == sectionId)
            .Select(assignment => assignment.SortOrder)
            .DefaultIfEmpty(item.SortOrder)
            .First();

    private static MenuEditorTab ParseEditorTab(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "drinks" => MenuEditorTab.Drinks,
            "hours" => MenuEditorTab.Hours,
            _ => MenuEditorTab.Food
        };

    private static MenuTab? ParseFoodFilter(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "breakfast" => MenuTab.Breakfast,
            "lunch" => MenuTab.Lunch,
            "dinner" => MenuTab.Dinner,
            _ => null
        };

    private static string GetEditorTabQueryValue(MenuEditorTab tab) =>
        tab switch
        {
            MenuEditorTab.Drinks => "drinks",
            MenuEditorTab.Hours => "hours",
            _ => "food"
        };

    private static string GetTabQueryValue(MenuTab tab) =>
        tab switch
        {
            MenuTab.Breakfast => "breakfast",
            MenuTab.Dinner => "dinner",
            MenuTab.Drinks => "drinks",
            _ => "lunch"
        };

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;

    private static string? FormatDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", InvariantCulture);

    private static string? FormatTime(TimeOnly? value) => value is null ? null : FlexibleTimeText.FormatDisplay(value.Value);

    private static bool TryValidateHoursForm(MenuServiceWindowFormModel model, out string? error)
    {
        foreach (var day in model.Days)
        {
            if (!day.IsAvailable)
            {
                continue;
            }

            if (!TryParseRequiredTime(day.OpensAtText, $"{GetDayLabel(day.DayOfWeek)} opening time", out _, out error)
                || !TryParseRequiredTime(day.ClosesAtText, $"{GetDayLabel(day.DayOfWeek)} closing time", out _, out error))
            {
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryParseRequiredDate(string? value, string fieldName, out DateOnly? date, out string? error)
    {
        date = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"{fieldName} is required.";
            return false;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            date = parsedDate;
            return true;
        }

        error = $"{fieldName} must use a valid date.";
        return false;
    }

    private static bool TryParseOptionalDate(string? value, string fieldName, out DateOnly? date, out string? error)
    {
        date = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            date = parsedDate;
            return true;
        }

        error = $"{fieldName} must use a valid date.";
        return false;
    }

    private static bool TryParseRequiredTime(string? value, string fieldName, out TimeOnly? time, out string? error)
    {
        time = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"{fieldName} is required when that day is available.";
            return false;
        }

        if (FlexibleTimeText.TryParse(value, out var parsedTime))
        {
            time = parsedTime;
            return true;
        }

        error = $"{fieldName} must use a valid time.";
        return false;
    }

    private static bool TryParseOptionalTime(string? value, string fieldName, out TimeOnly? time, out string? error)
    {
        time = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (FlexibleTimeText.TryParse(value, out var parsedTime))
        {
            time = parsedTime;
            return true;
        }

        error = $"{fieldName} must use a valid time.";
        return false;
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string GetHoursInputId(DayOfWeek dayOfWeek, string slot) =>
        $"menu-hours-{dayOfWeek.ToString().ToLowerInvariant()}-{slot}";

    private static int GetMaxDayForMonth(int? month, bool isStartBoundary) =>
        month is null
            ? 31
            : MenuAvailabilityRules.GetEffectiveDay(2024, month.Value, null, isStartBoundary);

    private IEnumerable<MenuTab> GetSelectedSectionTabs()
    {
        if (sectionForm.Family == MenuFamily.Drink)
        {
            if (sectionForm.ShowDrinks)
            {
                yield return MenuTab.Drinks;
            }

            yield break;
        }

        if (sectionForm.ShowBreakfast)
        {
            yield return MenuTab.Breakfast;
        }

        if (sectionForm.ShowLunch)
        {
            yield return MenuTab.Lunch;
        }

        if (sectionForm.ShowDinner)
        {
            yield return MenuTab.Dinner;
        }
    }

    private IEnumerable<MenuTab> GetAllowedSectionTabsForItem() =>
        Sections
            .Where(section => itemForm.SelectedSectionIds.Contains(section.SectionId))
            .SelectMany(section => section.MenuTabs)
            .Distinct()
            .OrderBy(tab => tab);

    private IEnumerable<MenuTab> GetSelectedMenuTabs()
    {
        if (GetItemEditorFamily() == MenuFamily.Drink)
        {
            yield return MenuTab.Drinks;
            yield break;
        }

        if (itemForm.ShowBreakfast)
        {
            yield return MenuTab.Breakfast;
        }

        if (itemForm.ShowLunch)
        {
            yield return MenuTab.Lunch;
        }

        if (itemForm.ShowDinner)
        {
            yield return MenuTab.Dinner;
        }
    }

    private static string GetTabLabel(MenuTab tab) =>
        tab switch
        {
            MenuTab.Breakfast => "Breakfast",
            MenuTab.Lunch => "Lunch",
            MenuTab.Dinner => "Dinner",
            MenuTab.Drinks => "Drinks",
            _ => tab.ToString()
        };

    private static string GetFamilyLabel(MenuFamily family) =>
        family switch
        {
            MenuFamily.Food => "Food",
            MenuFamily.Drink => "Drink",
            _ => family.ToString()
        };

    private static string GetDayLabel(DayOfWeek dayOfWeek) =>
        dayOfWeek switch
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

    private static string GetStatusClass(string label) =>
        label switch
        {
            "Coming Soon" => "status-pill--coming-soon",
            "Seasonal" => "status-pill--seasonal",
            "Today" => "status-pill--today",
            "Archived" => "status-pill--schedule",
            "Hidden" => "status-pill--limited",
            "Expired" => "status-pill--limited",
            _ => "status-pill--schedule"
        };

    private static string GetPriceSummary(MenuItemAdminView item) =>
        item.PriceVariants.Count == 1
            ? item.PriceVariants[0].PriceDisplay
            : string.Join(" / ", item.PriceVariants.Select(variant => $"{variant.Label} {variant.PriceDisplay}"));

    private static string GetItemTabSummary(MenuItemAdminView item)
    {
        if (item.Family == MenuFamily.Drink)
        {
            return "Drinks";
        }

        if (item.UsesSectionVisibility)
        {
            return "Uses section defaults";
        }

        var labels = item.MenuTabs
            .OrderBy(tab => tab)
            .Select(GetTabLabel)
            .ToArray();

        return labels.Length == 0 ? "No guest tab" : string.Join(", ", labels);
    }

    private static string GetItemBrowserSubtitle(MenuItemAdminView item)
    {
        if (item.Special is null)
        {
            return GetItemTabSummary(item);
        }

        return item.Special.TimeSummary is { Length: > 0 } timeSummary
            ? $"{item.Special.ScheduleSummary} | {timeSummary}"
            : item.Special.ScheduleSummary;
    }

    private string GetEditorTabClass(MenuEditorTab tab) =>
        selectedEditorTab == tab
            ? "menu-editor-tabs__button is-selected"
            : "menu-editor-tabs__button";

    private static string GetChoiceChipClass(bool isSelected, bool isDisabled = false) =>
        string.Join(' ',
            new[]
            {
                "chip",
                isSelected ? "chip--info menu-editor-filter-chip is-selected" : "chip--neutral menu-editor-filter-chip",
                isDisabled ? "is-disabled" : null
            }.Where(value => value is not null));

    private string GetFoodFilterClass(MenuTab? filter) =>
        selectedFoodFilter == filter
            ? "chip chip--info menu-editor-filter-chip is-selected"
            : "chip chip--neutral menu-editor-filter-chip";

    private string GetContentFilterClass(MenuContentFilter filter) =>
        GetCurrentContentFilter() == filter
            ? "chip chip--info menu-editor-filter-chip is-selected"
            : "chip chip--neutral menu-editor-filter-chip";

    private string GetArchiveFilterClass(MenuArchiveFilter filter) =>
        GetCurrentArchiveFilter() == filter
            ? "menu-editor-segmented__button is-selected"
            : "menu-editor-segmented__button";

    private MenuContentFilter GetCurrentContentFilter() =>
        selectedEditorTab == MenuEditorTab.Drinks ? drinkContentFilter : foodContentFilter;

    private MenuArchiveFilter GetCurrentArchiveFilter() =>
        selectedEditorTab == MenuEditorTab.Drinks ? drinkArchiveFilter : foodArchiveFilter;

    private string GetSectionContainerClass(MenuAdminBrowserSectionViewModel browserSection)
    {
        List<string> classes = ["menu-editor-tree__section"];

        if (IsBrowserSectionExpanded(browserSection.Section.SectionId))
        {
            classes.Add("is-expanded");
        }

        if (browserSection.Section.IsArchived)
        {
            classes.Add("is-archived");
        }
        else if (!browserSection.Section.IsVisibleToGuests)
        {
            classes.Add("is-hidden");
        }

        if (browserSection.IsContextMuted)
        {
            classes.Add("is-context-muted");
        }

        return string.Join(' ', classes);
    }

    private bool IsBrowserSectionExpanded(Guid sectionId) => expandedBrowserSectionIds.Contains(sectionId);

    private void ToggleBrowserSection(MenuSectionAdminView section)
    {
        SelectSection(section);
        ToggleBrowserSectionExpanded(section.SectionId);
    }

    private void ToggleBrowserSectionExpanded(Guid sectionId)
    {
        if (!expandedBrowserSectionIds.Add(sectionId))
        {
            expandedBrowserSectionIds.Remove(sectionId);
        }
    }

    private void HandleBrowserSectionHeaderKeyDown(KeyboardEventArgs args, MenuSectionAdminView section)
    {
        if (args.Key is "Enter" or " " or "Spacebar")
        {
            ToggleBrowserSection(section);
        }
    }

    private string GetTreeRowClass(MenuAdminDetailKind kind, Guid id, bool isArchived, bool isVisibleToGuests, bool isContextMuted = false)
    {
        List<string> classes = ["menu-editor-tree__row"];

        if (detailKind == kind && selectedBrowserId == id)
        {
            classes.Add("is-selected");
        }

        if (isArchived)
        {
            classes.Add("is-archived");
        }
        else if (!isVisibleToGuests)
        {
            classes.Add("is-hidden");
        }

        if (isContextMuted)
        {
            classes.Add("is-context-muted");
        }

        return string.Join(' ', classes);
    }

    private sealed record MenuBrowserDragState(MenuAdminDetailKind Kind, Guid RecordId, Guid? SectionId, MenuFamily Family, bool IsSpecialGroup);
}
