using System.Globalization;
using Anchor.Domain.Menu;
using Anchor.Web.Components.Pages;
using Anchor.Web.Components.Shared;
using Microsoft.AspNetCore.Components;

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
    private MenuRecurringSpecialFormModel specialForm = new();
    private MenuServiceWindowFormModel serviceWindowForm = new();
    private string? statusMessage;
    private bool isLoading = true;
    private MenuEditorTab selectedEditorTab = MenuEditorTab.Food;
    private MenuTab? selectedFoodFilter;
    private MenuArchiveFilter foodArchiveFilter = MenuArchiveFilter.Active;
    private MenuArchiveFilter drinkArchiveFilter = MenuArchiveFilter.Active;
    private MenuAdminDetailKind detailKind;
    private Guid? selectedBrowserId;
    private Guid? pendingSectionDeleteId;
    private Guid? pendingItemDeleteId;
    private Guid? pendingSpecialDeleteId;
    private MenuBrowserDragState? dragState;
    private string sectionSnapshot = string.Empty;
    private string itemSnapshot = string.Empty;
    private string specialSnapshot = string.Empty;
    private string hoursSnapshot = string.Empty;

    [Inject]
    private IMenuQueryService MenuQueryService { get; set; } = null!;

    [Inject]
    private IMenuManagementService MenuManagementService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "tab")]
    private string? RequestedEditorTab { get; set; }

    [SupplyParameterFromQuery(Name = "food")]
    private string? RequestedFoodFilter { get; set; }

    private IReadOnlyList<MenuSectionAdminView> Sections => menuView?.Sections ?? [];

    private IReadOnlyList<MenuItemAdminView> Items => menuView?.Items ?? [];

    private IReadOnlyList<RecurringSpecialAdminView> Specials => menuView?.RecurringSpecials ?? [];

    private IReadOnlyList<MenuTabHoursAdminView> HourTabs => menuView?.Tabs ?? [];

    private MenuFamily CurrentContentFamily => selectedEditorTab == MenuEditorTab.Drinks ? MenuFamily.Drink : MenuFamily.Food;

    private string CurrentWorkspaceLabel => selectedEditorTab == MenuEditorTab.Drinks ? "Drinks" : "Food";

    private IReadOnlyList<MenuAdminBrowserSectionViewModel> BrowserSections =>
        selectedEditorTab == MenuEditorTab.Drinks
            ? BuildBrowserSections(MenuFamily.Drink, null, drinkArchiveFilter)
            : BuildBrowserSections(MenuFamily.Food, selectedFoodFilter, foodArchiveFilter);

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

    private IReadOnlyList<MenuSectionAdminView> SpecialSectionOptions =>
        Sections
            .Where(section => section.Family == GetFamilyForTab(specialForm.Tab))
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyList<MenuItemAdminView> SpecialLinkedItemOptions
    {
        get
        {
            var sectionId = ParseGuid(specialForm.SectionId);
            if (sectionId is null)
            {
                return [];
            }

            return Items
                .Where(item => item.SectionId == sectionId.Value)
                .Where(item => item.Family == GetFamilyForTab(specialForm.Tab))
                .Where(item => specialForm.Tab == MenuTab.Drinks || item.FoodTabs.Contains(specialForm.Tab))
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    private IReadOnlyList<MenuTab> SpecialTabOptions =>
        selectedEditorTab == MenuEditorTab.Drinks
            ? [MenuTab.Drinks]
            : [MenuTab.Breakfast, MenuTab.Lunch, MenuTab.Dinner];

    private bool HasItemCreationContext => Sections.Any(section => section.Family == CurrentContentFamily);

    private bool HasSpecialCreationContext => Sections.Any(section => section.Family == CurrentContentFamily);

    private bool SectionHasUnsavedChanges => BuildSectionSnapshot(sectionForm) != sectionSnapshot;

    private bool ItemHasUnsavedChanges => BuildItemSnapshot(itemForm) != itemSnapshot;

    private bool SpecialHasUnsavedChanges => BuildSpecialSnapshot(specialForm) != specialSnapshot;

    private bool HoursHaveUnsavedChanges => BuildHoursSnapshot(serviceWindowForm) != hoursSnapshot;

    private bool CurrentDetailHasUnsavedChanges =>
        detailKind switch
        {
            MenuAdminDetailKind.Section => SectionHasUnsavedChanges,
            MenuAdminDetailKind.Item => ItemHasUnsavedChanges,
            MenuAdminDetailKind.Special => SpecialHasUnsavedChanges,
            _ => false
        };

    private string CurrentDetailTitle =>
        detailKind switch
        {
            MenuAdminDetailKind.Section => sectionForm.SectionId is null ? $"New {GetFamilyLabel(CurrentContentFamily)} section" : $"Edit section: {sectionForm.Name}",
            MenuAdminDetailKind.Item => itemForm.ItemId is null ? $"New {GetFamilyLabel(CurrentContentFamily)} item" : $"Edit item: {itemForm.Name}",
            MenuAdminDetailKind.Special => specialForm.SpecialId is null ? $"New {GetFamilyLabel(CurrentContentFamily)} special" : $"Edit special: {specialForm.Title}",
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
            ResetSpecialForm();
            ResetHoursForm();
        }

        EnsureSelectionForCurrentTab();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        menuView = await MenuQueryService.GetMenuManagementViewAsync(today);
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
            MenuAdminDetailKind.Special => GetFamilyForTab(specialForm.Tab) == CurrentContentFamily,
            _ => false
        };

    private bool CurrentDetailVisibleInBrowser() =>
        detailKind switch
        {
            MenuAdminDetailKind.Section when sectionForm.SectionId is Guid sectionId => BrowserSections.Any(section => section.Section.SectionId == sectionId),
            MenuAdminDetailKind.Item when itemForm.ItemId is Guid itemId => BrowserSections.Any(section => section.Items.Any(item => item.ItemId == itemId)),
            MenuAdminDetailKind.Special when specialForm.SpecialId is Guid specialId => BrowserSections.Any(section => section.Specials.Any(special => special.SpecialId == specialId)),
            _ => false
        };

    private bool IsCreatingNewRecord() =>
        detailKind switch
        {
            MenuAdminDetailKind.Section => sectionForm.SectionId is null,
            MenuAdminDetailKind.Item => itemForm.ItemId is null,
            MenuAdminDetailKind.Special => specialForm.SpecialId is null,
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
        CaptureSectionSnapshot();
    }

    private void ResetItemForm()
    {
        itemForm = CreateDefaultItemForm();
        itemForm.SectionId = Sections
            .Where(section => section.Family == MenuFamily.Food)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .Select(section => section.SectionId.ToString())
            .FirstOrDefault();
        NormalizeItemFormForSelectedSection();
        CaptureItemSnapshot();
    }

    private void ResetSpecialForm()
    {
        specialForm = new MenuRecurringSpecialFormModel
        {
            SectionId = Sections
                .Where(section => section.Family == MenuFamily.Food)
                .OrderBy(section => section.SortOrder)
                .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
                .Select(section => section.SectionId.ToString())
                .FirstOrDefault()
        };
        NormalizeSpecialFormSelection();
        CaptureSpecialSnapshot();
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
        detailKind = MenuAdminDetailKind.Section;
        selectedBrowserId = null;
        CaptureSectionSnapshot();
    }

    private void StartNewSectionForCurrentTab() => StartNewSection(CurrentContentFamily);

    private void StartNewItem(MenuFamily family)
    {
        ClearPendingDeletes();
        itemForm = CreateDefaultItemForm();

        var contextSection = ResolveContextSection(family);
        itemForm.SectionId = contextSection?.SectionId.ToString();
        itemForm.SortOrder = GetNextItemSortOrder(contextSection?.SectionId);

        if (family == MenuFamily.Food)
        {
            ApplyFoodFilterDefaultsToItemForm();
        }

        NormalizeItemFormForSelectedSection();
        detailKind = MenuAdminDetailKind.Item;
        selectedBrowserId = null;
        CaptureItemSnapshot();
    }

    private void StartNewItemForCurrentTab() => StartNewItem(CurrentContentFamily);

    private void StartNewSpecial(MenuFamily family)
    {
        ClearPendingDeletes();
        specialForm = new MenuRecurringSpecialFormModel
        {
            Tab = family == MenuFamily.Drink ? MenuTab.Drinks : selectedFoodFilter ?? MenuTab.Dinner
        };

        var contextSection = ResolveContextSection(family);
        specialForm.SectionId = contextSection?.SectionId.ToString();
        specialForm.SortOrder = GetNextSpecialSortOrder(contextSection?.SectionId);
        NormalizeSpecialFormSelection();

        detailKind = MenuAdminDetailKind.Special;
        selectedBrowserId = null;
        CaptureSpecialSnapshot();
    }

    private void StartNewSpecialForCurrentTab() => StartNewSpecial(CurrentContentFamily);

    private void StartNewItemFromSection(MenuSectionAdminView section)
    {
        SelectSection(section);
        StartNewItem(section.Family);
    }

    private void StartNewSpecialFromSection(MenuSectionAdminView section)
    {
        SelectSection(section);
        StartNewSpecial(section.Family);
    }

    private void ApplyFoodFilterDefaultsToItemForm()
    {
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
            MenuAdminDetailKind.Item => ParseGuid(itemForm.SectionId),
            MenuAdminDetailKind.Special => ParseGuid(specialForm.SectionId),
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

    private int GetNextItemSortOrder(Guid? sectionId) =>
        sectionId is Guid id
            ? Items.Where(item => item.SectionId == id).Select(item => item.SortOrder).DefaultIfEmpty(0).Max() + 1
            : 1;

    private int GetNextSpecialSortOrder(Guid? sectionId) =>
        sectionId is Guid id
            ? Specials.Where(special => special.SectionId == id).Select(special => special.SortOrder).DefaultIfEmpty(0).Max() + 1
            : 1;

    private void SelectSection(MenuSectionAdminView section)
    {
        ClearPendingDeletes();
        detailKind = MenuAdminDetailKind.Section;
        selectedBrowserId = section.SectionId;
        sectionForm = new MenuSectionFormModel
        {
            SectionId = section.SectionId,
            Name = section.Name,
            Family = section.Family,
            SortOrder = section.SortOrder,
            IsVisibleToGuests = section.IsVisibleToGuests,
            IsArchived = section.IsArchived
        };
        CaptureSectionSnapshot();
    }

    private void SelectItem(MenuItemAdminView item)
    {
        ClearPendingDeletes();
        detailKind = MenuAdminDetailKind.Item;
        selectedBrowserId = item.ItemId;
        itemForm = new MenuItemFormModel
        {
            ItemId = item.ItemId,
            SectionId = item.SectionId.ToString(),
            Name = item.Name,
            Description = item.Description,
            ImagePath = item.ImagePath,
            SortOrder = item.SortOrder,
            IsVisibleToGuests = item.IsVisibleToGuests,
            IsArchived = item.IsArchived,
            OfferStartsOnText = FormatDate(item.OfferStartsOn),
            OfferEndsOnText = FormatDate(item.OfferEndsOn),
            IsSeasonal = item.IsSeasonal,
            ShowBreakfast = item.FoodTabs.Contains(MenuTab.Breakfast),
            ShowLunch = item.FoodTabs.Contains(MenuTab.Lunch),
            ShowDinner = item.FoodTabs.Contains(MenuTab.Dinner)
        };
        itemForm.PriceVariants.Clear();
        foreach (var variant in item.PriceVariants)
        {
            itemForm.PriceVariants.Add(new MenuItemPriceVariantFormModel
            {
                Label = variant.Label,
                AmountText = variant.Amount.ToString("0.00", InvariantCulture),
                SortOrder = variant.SortOrder
            });
        }

        NormalizeItemFormForSelectedSection();
        CaptureItemSnapshot();
    }

    private void SelectSpecial(RecurringSpecialAdminView special)
    {
        ClearPendingDeletes();
        detailKind = MenuAdminDetailKind.Special;
        selectedBrowserId = special.SpecialId;
        specialForm = new MenuRecurringSpecialFormModel
        {
            SpecialId = special.SpecialId,
            Tab = special.Tab,
            SectionId = special.SectionId.ToString(),
            DayOfWeek = special.DayOfWeek,
            Title = special.Title,
            Description = special.Description,
            TimeNote = special.TimeNote,
            PriceNote = special.PriceNote,
            LinkedMenuItemId = special.LinkedMenuItemId?.ToString(),
            SortOrder = special.SortOrder,
            IsVisibleToGuests = special.IsVisibleToGuests,
            IsArchived = special.IsArchived
        };
        NormalizeSpecialFormSelection();
        CaptureSpecialSnapshot();
    }

    private async Task SaveSectionAsync()
    {
        var currentSectionId = sectionForm.SectionId;
        var result = await MenuManagementService.SaveSectionAsync(
            new SaveMenuSectionRequest(
                sectionForm.SectionId,
                sectionForm.Name,
                sectionForm.Family,
                sectionForm.SortOrder,
                sectionForm.IsVisibleToGuests,
                sectionForm.IsArchived));

        if (!await HandleOperationResultAsync(result, currentSectionId is null ? "Section created." : "Section updated."))
        {
            return;
        }

        TrySelectSectionById(result.EntityId ?? currentSectionId);
    }

    private async Task SaveItemAsync()
    {
        var sectionId = ParseGuid(itemForm.SectionId);
        if (sectionId is null)
        {
            statusMessage = "Error: Choose a section before saving the menu item.";
            return;
        }

        if (!TryParseOptionalDate(itemForm.OfferStartsOnText, "Offer start date", out var offerStartsOn, out var dateError)
            || !TryParseOptionalDate(itemForm.OfferEndsOnText, "Offer end date", out var offerEndsOn, out dateError))
        {
            statusMessage = $"Error: {dateError}";
            return;
        }

        var priceVariants = new List<SaveMenuItemPriceVariantRequest>(itemForm.PriceVariants.Count);
        foreach (var variant in itemForm.PriceVariants)
        {
            if (!decimal.TryParse(variant.AmountText, NumberStyles.Number, InvariantCulture, out var amount))
            {
                statusMessage = $"Error: Enter a valid number for the '{variant.Label}' price.";
                return;
            }

            priceVariants.Add(new SaveMenuItemPriceVariantRequest(null, variant.Label, amount, variant.SortOrder));
        }

        var currentItemId = itemForm.ItemId;
        var result = await MenuManagementService.SaveItemAsync(
            new SaveMenuItemRequest(
                itemForm.ItemId,
                sectionId.Value,
                itemForm.Name,
                itemForm.Description,
                itemForm.ImagePath,
                itemForm.SortOrder,
                itemForm.IsVisibleToGuests,
                itemForm.IsArchived,
                offerStartsOn,
                offerEndsOn,
                itemForm.IsSeasonal,
                priceVariants,
                GetItemEditorFamily() == MenuFamily.Food ? GetSelectedFoodTabs().ToArray() : Array.Empty<MenuTab>()));

        if (!await HandleOperationResultAsync(result, currentItemId is null ? "Menu item created." : "Menu item updated."))
        {
            return;
        }

        TrySelectItemById(result.EntityId ?? currentItemId);
    }

    private async Task SaveRecurringSpecialAsync()
    {
        var sectionId = ParseGuid(specialForm.SectionId);
        if (sectionId is null)
        {
            statusMessage = "Error: Choose a section before saving the recurring special.";
            return;
        }

        Guid? linkedMenuItemId = null;
        if (!string.IsNullOrWhiteSpace(specialForm.LinkedMenuItemId))
        {
            linkedMenuItemId = ParseGuid(specialForm.LinkedMenuItemId);
            if (linkedMenuItemId is null)
            {
                statusMessage = "Error: Choose a valid featured menu item.";
                return;
            }
        }

        var currentSpecialId = specialForm.SpecialId;
        var result = await MenuManagementService.SaveRecurringSpecialAsync(
            new SaveRecurringSpecialRequest(
                specialForm.SpecialId,
                specialForm.Tab,
                sectionId.Value,
                specialForm.DayOfWeek,
                specialForm.Title,
                specialForm.Description,
                specialForm.TimeNote,
                specialForm.PriceNote,
                linkedMenuItemId,
                specialForm.SortOrder,
                specialForm.IsVisibleToGuests,
                specialForm.IsArchived));

        if (!await HandleOperationResultAsync(result, currentSpecialId is null ? "Recurring special created." : "Recurring special updated."))
        {
            return;
        }

        TrySelectSpecialById(result.EntityId ?? currentSpecialId);
    }

    private async Task SaveServiceHoursAsync()
    {
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

    private async Task ArchiveSpecialAsync(Guid specialId)
    {
        var result = await MenuManagementService.ArchiveRecurringSpecialAsync(specialId);
        if (await HandleOperationResultAsync(result, "Recurring special archived."))
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
        pendingSpecialDeleteId = null;
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
        pendingSpecialDeleteId = null;
    }

    private async Task ToggleSpecialDeleteAsync(Guid specialId)
    {
        if (pendingSpecialDeleteId == specialId)
        {
            pendingSpecialDeleteId = null;
            var result = await MenuManagementService.DeleteRecurringSpecialAsync(specialId);
            if (await HandleOperationResultAsync(result, "Recurring special deleted."))
            {
                if (specialForm.SpecialId == specialId)
                {
                    detailKind = MenuAdminDetailKind.None;
                    selectedBrowserId = null;
                }

                EnsureSelectionForCurrentTab();
            }

            return;
        }

        pendingSpecialDeleteId = specialId;
        pendingSectionDeleteId = null;
        pendingItemDeleteId = null;
    }

    private async Task HandleItemSectionChangedAsync(ChangeEventArgs args)
    {
        itemForm.SectionId = args.Value?.ToString();
        itemForm.SortOrder = GetNextItemSortOrder(ParseGuid(itemForm.SectionId));
        NormalizeItemFormForSelectedSection();
        await Task.CompletedTask;
    }

    private async Task HandleSpecialTabChangedAsync(ChangeEventArgs args)
    {
        if (Enum.TryParse<MenuTab>(args.Value?.ToString(), out var selectedTab))
        {
            specialForm.Tab = selectedTab;
        }

        NormalizeSpecialFormSelection();
        specialForm.SortOrder = GetNextSpecialSortOrder(ParseGuid(specialForm.SectionId));
        await Task.CompletedTask;
    }

    private async Task HandleSpecialSectionChangedAsync(ChangeEventArgs args)
    {
        specialForm.SectionId = args.Value?.ToString();
        specialForm.SortOrder = GetNextSpecialSortOrder(ParseGuid(specialForm.SectionId));
        NormalizeSpecialFormSelection();
        await Task.CompletedTask;
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
    }

    private void NormalizeItemFormForSelectedSection()
    {
        if (GetItemEditorFamily() != MenuFamily.Drink)
        {
            return;
        }

        itemForm.ShowBreakfast = false;
        itemForm.ShowLunch = false;
        itemForm.ShowDinner = false;
    }

    private void NormalizeSpecialFormSelection()
    {
        var allowedSectionIds = SpecialSectionOptions
            .Select(section => section.SectionId.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (specialForm.SectionId is null || !allowedSectionIds.Contains(specialForm.SectionId))
        {
            specialForm.SectionId = allowedSectionIds.FirstOrDefault();
        }

        if (specialForm.LinkedMenuItemId is null)
        {
            return;
        }

        var linkedItemAllowed = SpecialLinkedItemOptions
            .Any(item => string.Equals(item.ItemId.ToString(), specialForm.LinkedMenuItemId, StringComparison.OrdinalIgnoreCase));

        if (!linkedItemAllowed)
        {
            specialForm.LinkedMenuItemId = null;
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

    private void ReloadSelectedDetailOrEnsureSelection()
    {
        if (!TryReloadCurrentDetailFromData())
        {
            EnsureSelectionForCurrentTab();
        }
    }

    private bool TryReloadCurrentDetailFromData()
    {
        switch (detailKind)
        {
            case MenuAdminDetailKind.Section when sectionForm.SectionId is Guid sectionId:
                return TrySelectSectionById(sectionId);
            case MenuAdminDetailKind.Item when itemForm.ItemId is Guid itemId:
                return TrySelectItemById(itemId);
            case MenuAdminDetailKind.Special when specialForm.SpecialId is Guid specialId:
                return TrySelectSpecialById(specialId);
            default:
                return false;
        }
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

        SelectItem(item);
        return true;
    }

    private bool TrySelectSpecialById(Guid? specialId)
    {
        if (specialId is not Guid id)
        {
            return false;
        }

        var special = Specials.SingleOrDefault(entry => entry.SpecialId == id);
        if (special is null)
        {
            return false;
        }

        SelectSpecial(special);
        return true;
    }

    private void ClearPendingDeletes()
    {
        pendingSectionDeleteId = null;
        pendingItemDeleteId = null;
        pendingSpecialDeleteId = null;
    }

    private IReadOnlyList<MenuAdminBrowserSectionViewModel> BuildBrowserSections(
        MenuFamily family,
        MenuTab? foodFilter,
        MenuArchiveFilter archiveFilter)
    {
        var matchingItems = Items
            .Where(item => item.Family == family)
            .Where(item => MatchesFoodFilter(item, foodFilter))
            .ToLookup(item => item.SectionId);

        var matchingSpecials = Specials
            .Where(special => GetFamilyForTab(special.Tab) == family)
            .Where(special => MatchesFoodFilter(special, foodFilter))
            .ToLookup(special => special.SectionId);

        List<MenuAdminBrowserSectionViewModel> browserSections = [];

        foreach (var section in Sections
                     .Where(section => section.Family == family)
                     .OrderBy(section => section.SortOrder)
                     .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase))
        {
            var visibleItems = matchingItems[section.SectionId]
                .Where(item => MatchesArchiveFilter(item.IsArchived, archiveFilter))
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var visibleSpecials = matchingSpecials[section.SectionId]
                .Where(special => MatchesArchiveFilter(special.IsArchived, archiveFilter))
                .OrderBy(special => special.DayOfWeek)
                .ThenBy(special => special.SortOrder)
                .ThenBy(special => special.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var sectionMatches = MatchesArchiveFilter(section.IsArchived, archiveFilter);
            if (!sectionMatches && visibleItems.Length == 0 && visibleSpecials.Length == 0)
            {
                continue;
            }

            browserSections.Add(new MenuAdminBrowserSectionViewModel(
                section,
                IsContextMuted: !sectionMatches && archiveFilter != MenuArchiveFilter.Both,
                Items: visibleItems,
                Specials: visibleSpecials));
        }

        return browserSections;
    }

    private static bool MatchesArchiveFilter(bool isArchived, MenuArchiveFilter archiveFilter) =>
        archiveFilter switch
        {
            MenuArchiveFilter.Active => !isArchived,
            MenuArchiveFilter.Archived => isArchived,
            _ => true
        };

    private static bool MatchesFoodFilter(MenuItemAdminView item, MenuTab? foodFilter) =>
        item.Family == MenuFamily.Drink || foodFilter is null || item.FoodTabs.Contains(foodFilter.Value);

    private static bool MatchesFoodFilter(RecurringSpecialAdminView special, MenuTab? foodFilter) =>
        special.Tab == MenuTab.Drinks || foodFilter is null || special.Tab == foodFilter.Value;

    private void BeginSectionDrag(MenuSectionAdminView section) =>
        dragState = new MenuBrowserDragState(MenuAdminDetailKind.Section, section.SectionId, null, section.Family);

    private void BeginItemDrag(MenuItemAdminView item) =>
        dragState = new MenuBrowserDragState(MenuAdminDetailKind.Item, item.ItemId, item.SectionId, item.Family);

    private void BeginSpecialDrag(RecurringSpecialAdminView special) =>
        dragState = new MenuBrowserDragState(MenuAdminDetailKind.Special, special.SpecialId, special.SectionId, GetFamilyForTab(special.Tab));

    private async Task DropSectionAsync(MenuSectionAdminView targetSection)
    {
        if (dragState is not { Kind: MenuAdminDetailKind.Section } state
            || state.RecordId == targetSection.SectionId
            || state.Family != targetSection.Family)
        {
            return;
        }

        dragState = null;
        await ReorderSectionsAsync(state.RecordId, targetSection.SectionId, targetSection.Family);
    }

    private async Task DropItemAsync(MenuItemAdminView targetItem)
    {
        if (dragState is not { Kind: MenuAdminDetailKind.Item, ParentSectionId: { } parentSectionId } state
            || state.RecordId == targetItem.ItemId
            || parentSectionId != targetItem.SectionId)
        {
            return;
        }

        dragState = null;
        await ReorderItemsAsync(state.RecordId, targetItem.ItemId, targetItem.SectionId);
    }

    private async Task DropSpecialAsync(RecurringSpecialAdminView targetSpecial)
    {
        if (dragState is not { Kind: MenuAdminDetailKind.Special, ParentSectionId: { } parentSectionId } state
            || state.RecordId == targetSpecial.SpecialId
            || parentSectionId != targetSpecial.SectionId)
        {
            return;
        }

        dragState = null;
        await ReorderSpecialsAsync(state.RecordId, targetSpecial.SpecialId, targetSpecial.SectionId);
    }

    private async Task MoveSectionAsync(MenuSectionAdminView section, int direction)
    {
        var siblings = BrowserSections.Select(entry => entry.Section).ToArray();
        var currentIndex = Array.FindIndex(siblings, sibling => sibling.SectionId == section.SectionId);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= siblings.Length)
        {
            return;
        }

        await ReorderSectionsAsync(section.SectionId, siblings[targetIndex].SectionId, section.Family);
    }

    private async Task MoveItemAsync(MenuItemAdminView item, int direction)
    {
        var siblings = BrowserSections
            .Single(section => section.Section.SectionId == item.SectionId)
            .Items
            .ToArray();

        var currentIndex = Array.FindIndex(siblings, sibling => sibling.ItemId == item.ItemId);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= siblings.Length)
        {
            return;
        }

        await ReorderItemsAsync(item.ItemId, siblings[targetIndex].ItemId, item.SectionId);
    }

    private async Task MoveSpecialAsync(RecurringSpecialAdminView special, int direction)
    {
        var siblings = BrowserSections
            .Single(section => section.Section.SectionId == special.SectionId)
            .Specials
            .ToArray();

        var currentIndex = Array.FindIndex(siblings, sibling => sibling.SpecialId == special.SpecialId);
        var targetIndex = currentIndex + direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= siblings.Length)
        {
            return;
        }

        await ReorderSpecialsAsync(special.SpecialId, siblings[targetIndex].SpecialId, special.SectionId);
    }

    private async Task ReorderSectionsAsync(Guid sourceSectionId, Guid targetSectionId, MenuFamily family)
    {
        var orderedSections = Sections
            .Where(section => section.Family == family)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!MoveIntoTargetSlot(orderedSections, section => section.SectionId, sourceSectionId, targetSectionId))
        {
            return;
        }

        if (await PersistSectionOrderAsync(orderedSections, "Section order updated."))
        {
            ReloadSelectedDetailOrEnsureSelection();
        }
    }

    private async Task ReorderItemsAsync(Guid sourceItemId, Guid targetItemId, Guid sectionId)
    {
        var orderedItems = Items
            .Where(item => item.SectionId == sectionId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!MoveIntoTargetSlot(orderedItems, item => item.ItemId, sourceItemId, targetItemId))
        {
            return;
        }

        if (await PersistItemOrderAsync(orderedItems, "Menu item order updated."))
        {
            ReloadSelectedDetailOrEnsureSelection();
        }
    }

    private async Task ReorderSpecialsAsync(Guid sourceSpecialId, Guid targetSpecialId, Guid sectionId)
    {
        var orderedSpecials = Specials
            .Where(special => special.SectionId == sectionId)
            .OrderBy(special => special.SortOrder)
            .ThenBy(special => special.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!MoveIntoTargetSlot(orderedSpecials, special => special.SpecialId, sourceSpecialId, targetSpecialId))
        {
            return;
        }

        if (await PersistSpecialOrderAsync(orderedSpecials, "Recurring special order updated."))
        {
            ReloadSelectedDetailOrEnsureSelection();
        }
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

    private async Task<bool> PersistItemOrderAsync(IReadOnlyList<MenuItemAdminView> orderedItems, string successMessage)
    {
        List<SaveMenuSortOrderRequest> updates = [];
        for (var index = 0; index < orderedItems.Count; index++)
        {
            var item = orderedItems[index];
            var desiredSortOrder = index + 1;
            if (item.SortOrder != desiredSortOrder)
            {
                updates.Add(new SaveMenuSortOrderRequest(item.ItemId, desiredSortOrder));
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

    private async Task<bool> PersistSpecialOrderAsync(IReadOnlyList<RecurringSpecialAdminView> orderedSpecials, string successMessage)
    {
        List<SaveMenuSortOrderRequest> updates = [];
        for (var index = 0; index < orderedSpecials.Count; index++)
        {
            var special = orderedSpecials[index];
            var desiredSortOrder = index + 1;
            if (special.SortOrder != desiredSortOrder)
            {
                updates.Add(new SaveMenuSortOrderRequest(special.SpecialId, desiredSortOrder));
            }
        }

        if (updates.Count == 0)
        {
            return false;
        }

        var result = await MenuManagementService.ReorderRecurringSpecialsAsync(updates);
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

    private MenuFamily GetSectionEditorFamily() => sectionForm.Family;

    private MenuFamily GetItemEditorFamily()
    {
        var selectedSectionId = ParseGuid(itemForm.SectionId);
        var selectedSection = selectedSectionId is Guid id
            ? Sections.SingleOrDefault(section => section.SectionId == id)
            : null;

        return selectedSection?.Family ?? CurrentContentFamily;
    }

    private void CaptureSectionSnapshot() => sectionSnapshot = BuildSectionSnapshot(sectionForm);

    private void CaptureItemSnapshot() => itemSnapshot = BuildItemSnapshot(itemForm);

    private void CaptureSpecialSnapshot() => specialSnapshot = BuildSpecialSnapshot(specialForm);

    private void CaptureHoursSnapshot() => hoursSnapshot = BuildHoursSnapshot(serviceWindowForm);

    private static string BuildSectionSnapshot(MenuSectionFormModel model) =>
        string.Join("|", model.SectionId, model.Name, model.Family, model.SortOrder, model.IsVisibleToGuests, model.IsArchived);

    private static string BuildItemSnapshot(MenuItemFormModel model) =>
        string.Join("|",
            model.ItemId,
            model.SectionId,
            model.Name,
            model.Description,
            model.ImagePath,
            model.SortOrder,
            model.IsVisibleToGuests,
            model.IsArchived,
            model.OfferStartsOnText,
            model.OfferEndsOnText,
            model.IsSeasonal,
            model.ShowBreakfast,
            model.ShowLunch,
            model.ShowDinner,
            string.Join(";", model.PriceVariants.Select(variant => $"{variant.Label}~{variant.AmountText}~{variant.SortOrder}")));

    private static string BuildSpecialSnapshot(MenuRecurringSpecialFormModel model) =>
        string.Join("|",
            model.SpecialId,
            model.Tab,
            model.SectionId,
            model.DayOfWeek,
            model.Title,
            model.Description,
            model.TimeNote,
            model.PriceNote,
            model.LinkedMenuItemId,
            model.SortOrder,
            model.IsVisibleToGuests,
            model.IsArchived);

    private static string BuildHoursSnapshot(MenuServiceWindowFormModel model) =>
        string.Join("|",
            model.Tab,
            string.Join(";", model.Days.Select(day => $"{day.DayOfWeek}~{day.IsAvailable}~{day.OpensAtText}~{day.ClosesAtText}~{day.ClosesNextDay}")));

    private static MenuItemFormModel CreateDefaultItemForm() => new();

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

    private static MenuFamily GetFamilyForTab(MenuTab tab) => tab == MenuTab.Drinks ? MenuFamily.Drink : MenuFamily.Food;

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;

    private static string? FormatDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", InvariantCulture);

    private static string? FormatTime(TimeOnly? value) => value is null ? null : FlexibleTimeText.FormatDisplay(value.Value);

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

    private static string GetHoursInputId(DayOfWeek dayOfWeek, string slot) =>
        $"menu-hours-{dayOfWeek.ToString().ToLowerInvariant()}-{slot}";

    private IEnumerable<MenuTab> GetSelectedFoodTabs()
    {
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

        var labels = item.FoodTabs
            .OrderBy(tab => tab)
            .Select(GetTabLabel)
            .ToArray();

        return labels.Length == 0 ? "No guest tab" : string.Join(", ", labels);
    }

    private string GetEditorTabClass(MenuEditorTab tab) =>
        selectedEditorTab == tab
            ? "menu-editor-tabs__button is-selected"
            : "menu-editor-tabs__button";

    private string GetFoodFilterClass(MenuTab? filter) =>
        selectedFoodFilter == filter
            ? "chip chip--info menu-editor-filter-chip is-selected"
            : "chip chip--neutral menu-editor-filter-chip";

    private string GetArchiveFilterClass(MenuArchiveFilter filter)
    {
        var activeFilter = selectedEditorTab == MenuEditorTab.Drinks ? drinkArchiveFilter : foodArchiveFilter;
        return activeFilter == filter
            ? "menu-editor-segmented__button is-selected"
            : "menu-editor-segmented__button";
    }

    private string GetSectionContainerClass(MenuAdminBrowserSectionViewModel browserSection)
    {
        List<string> classes = ["menu-editor-tree__section"];

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

    private string GetTreeRowClass(MenuAdminDetailKind kind, Guid id, bool isArchived, bool isVisibleToGuests)
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

        return string.Join(' ', classes);
    }

    private sealed record MenuBrowserDragState(MenuAdminDetailKind Kind, Guid RecordId, Guid? ParentSectionId, MenuFamily Family);
}
