namespace Anchor.Domain.Menu;

public sealed class MenuManagementService(
    IMenuManagementRepository repository,
    IMenuOperationLogSink logSink) : IMenuManagementService
{
    public async Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default)
    {
        MenuSectionReferenceRecord? existingSection = null;
        MenuManagementSnapshot? snapshot = null;
        if (request.SectionId is { } sectionId)
        {
            existingSection = await repository.GetSectionReferenceAsync(sectionId, cancellationToken);
            if (existingSection is null)
            {
                return MenuOperationResult.Failure("The selected section could not be found.");
            }
        }

        var normalizedName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return MenuOperationResult.Failure("Section name is required.");
        }

        if (normalizedName.Length > 100)
        {
            return MenuOperationResult.Failure("Section name cannot be longer than 100 characters.");
        }

        var normalizedCallout = NormalizeOptionalValue(request.Callout);
        if (normalizedCallout is { Length: > 200 })
        {
            return MenuOperationResult.Failure("Section callout text cannot be longer than 200 characters.");
        }

        var normalizedSectionTabs = NormalizeSectionTabs(request.Family, request.MenuTabs);
        if (normalizedSectionTabs.Length == 0)
        {
            return MenuOperationResult.Failure(
                request.Family == MenuFamily.Drink
                    ? "Drink sections must appear on Drinks."
                    : "Food sections must appear on at least one of Breakfast, Lunch, or Dinner.");
        }

        if (request.SectionId is not null && request.ParentSectionId == request.SectionId)
        {
            return MenuOperationResult.Failure("A section cannot be its own parent.");
        }

        if (request.ParentSectionId is { } parentSectionId)
        {
            var parentSection = await repository.GetSectionReferenceAsync(parentSectionId, cancellationToken);
            if (parentSection is null)
            {
                return MenuOperationResult.Failure("Choose a valid parent section before saving.");
            }

            if (parentSection.Family != request.Family)
            {
                return MenuOperationResult.Failure("Parent and child sections must both belong to the same menu family.");
            }

            snapshot = await repository.GetMenuManagementSnapshotAsync(cancellationToken);
            if (request.SectionId is { } currentSectionId
                && IsSectionDescendant(snapshot.Sections, parentSectionId, currentSectionId))
            {
                return MenuOperationResult.Failure("A section cannot move under one of its own descendants.");
            }
        }

        var duplicateSectionId = await repository.FindSectionIdByNormalizedNameAsync(
            MenuNameRules.NormalizeForLookup(normalizedName),
            cancellationToken);
        if (duplicateSectionId is Guid foundSectionId && foundSectionId != request.SectionId)
        {
            return MenuOperationResult.Failure($"A section named {normalizedName} already exists. Rename this section before saving.");
        }

        if (existingSection is not null
            && existingSection.Family != request.Family
            && await repository.SectionHasDependentsAsync(existingSection.SectionId, cancellationToken))
        {
            return MenuOperationResult.Failure("Move or remove the section's items before changing it from food to drink or vice versa.");
        }

        var savedSectionId = await repository.UpsertSectionAsync(
            request with
            {
                Name = normalizedName,
                Callout = normalizedCallout,
                ParentSectionId = request.ParentSectionId,
                MenuTabs = normalizedSectionTabs
            },
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("save", "section", savedSectionId, normalizedName), cancellationToken);

        return MenuOperationResult.Success(savedSectionId);
    }

    public async Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSectionAssignments = request.SectionAssignments
            .GroupBy(assignment => assignment.SectionId)
            .Select(group => group.First() with { SortOrder = Math.Max(1, group.First().SortOrder) })
            .ToArray();
        var requestedSectionIds = normalizedSectionAssignments
            .Select(assignment => assignment.SectionId)
            .ToArray();
        if (requestedSectionIds.Length == 0)
        {
            return MenuOperationResult.Failure("Choose at least one section before saving the menu item.");
        }

        var sections = await repository.GetSectionReferencesAsync(requestedSectionIds, cancellationToken);
        if (sections.Count != requestedSectionIds.Length)
        {
            return MenuOperationResult.Failure("Select valid sections before saving the menu item.");
        }

        var family = sections[0].Family;
        if (sections.Any(section => section.Family != family))
        {
            return MenuOperationResult.Failure("A menu item cannot be assigned to both food and drink sections.");
        }

        if (request.ItemId is { } existingItemId)
        {
            var existingItem = await repository.GetItemReferenceAsync(existingItemId, cancellationToken);
            if (existingItem is null)
            {
                return MenuOperationResult.Failure("The selected menu item could not be found.");
            }
        }

        var normalizedName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return MenuOperationResult.Failure("Menu item name is required.");
        }

        if (normalizedName.Length > 150)
        {
            return MenuOperationResult.Failure("Menu item name cannot be longer than 150 characters.");
        }

        var duplicateItemId = await repository.FindItemIdByNormalizedNameAsync(
            MenuNameRules.NormalizeForLookup(normalizedName),
            cancellationToken);
        if (duplicateItemId is Guid foundItemId && foundItemId != request.ItemId)
        {
            return MenuOperationResult.Failure($"A menu item named {normalizedName} already exists. Rename this item or edit the existing one instead.");
        }

        var normalizedDescription = request.Description.Trim();
        if (normalizedDescription.Length > 1000)
        {
            return MenuOperationResult.Failure("Menu item description cannot be longer than 1000 characters.");
        }

        if (request.OfferStartsOn is not null
            && request.OfferEndsOn is not null
            && request.OfferEndsOn < request.OfferStartsOn)
        {
            return MenuOperationResult.Failure("Offer end date cannot be earlier than the offer start date.");
        }

        var normalizedSeasonalWindow = NormalizeSeasonalWindow(
            request.SeasonStartMonth,
            request.SeasonStartDay,
            request.SeasonEndMonth,
            request.SeasonEndDay,
            out var seasonalWindowError);
        if (seasonalWindowError is not null)
        {
            return MenuOperationResult.Failure(seasonalWindowError);
        }

        var normalizedPriceVariants = request.PriceVariants
            .Select(variant => variant with { Label = variant.Label.Trim() })
            .ToArray();

        if (normalizedPriceVariants.Length == 0)
        {
            return MenuOperationResult.Failure("Add at least one price variant before saving the menu item.");
        }

        if (normalizedPriceVariants.Any(variant => string.IsNullOrWhiteSpace(variant.Label)))
        {
            return MenuOperationResult.Failure("Each price variant needs a label.");
        }

        if (normalizedPriceVariants.Any(variant => variant.Amount <= 0))
        {
            return MenuOperationResult.Failure("Each price variant amount must be greater than zero.");
        }

        var allowedTabs = sections
            .SelectMany(section => section.MenuTabs)
            .Distinct()
            .OrderBy(tab => tab)
            .ToArray();

        var normalizedTabs = request.MenuTabs
            .Distinct()
            .OrderBy(tab => tab)
            .ToArray();

        if (family == MenuFamily.Food)
        {
            if (!request.UsesSectionVisibility && normalizedTabs.Length == 0)
            {
                return MenuOperationResult.Failure("Food items must appear on at least one of Breakfast, Lunch, or Dinner when section defaults are overridden.");
            }

            if (normalizedTabs.Any(tab => tab == MenuTab.Drinks))
            {
                return MenuOperationResult.Failure("Food items cannot be assigned to the Drinks tab.");
            }
        }
        else if (!request.UsesSectionVisibility && normalizedTabs.Length == 0)
        {
            return MenuOperationResult.Failure("Drink items must appear on Drinks when section defaults are overridden.");
        }
        else if (normalizedTabs.Any(tab => tab != MenuTab.Drinks))
        {
            return MenuOperationResult.Failure("Drink items cannot be assigned to Breakfast, Lunch, or Dinner.");
        }

        if (!request.UsesSectionVisibility && normalizedTabs.Except(allowedTabs).Any())
        {
            return MenuOperationResult.Failure("Item menu visibility cannot include menus that are not allowed by the selected sections.");
        }

        SaveMenuItemSpecialRequest? normalizedSpecial = null;
        if (request.Special is { } special)
        {
            var normalizedCallout = NormalizeOptionalValue(special.Callout);
            if (normalizedCallout is { Length: > 100 })
            {
                return MenuOperationResult.Failure("Special callouts cannot be longer than 100 characters.");
            }

            var normalizedSpecialDays = special.DaysOfWeek
                .Distinct()
                .OrderBy(day => Array.IndexOf(MenuPresentationRules.DayOrder.ToArray(), day))
                .ToArray();

            if (special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring && normalizedSpecialDays.Length == 0)
            {
                return MenuOperationResult.Failure("Choose at least one weekday for recurring specials.");
            }

            if (special.ScheduleKind == MenuItemSpecialScheduleKind.Dated && special.StartDate is null)
            {
                return MenuOperationResult.Failure("Choose a start date for dated specials.");
            }

            if (special.EndDate is not null && special.StartDate is not null && special.EndDate < special.StartDate)
            {
                return MenuOperationResult.Failure("Special end date cannot be earlier than the special start date.");
            }

            if (special.ClosesNextDay && special.EndsAt is null)
            {
                return MenuOperationResult.Failure("Closes next day can only be used when the special has an end time.");
            }

            if (special.StartsAt is { } startsAt
                && special.EndsAt is { } endsAt
                && !special.ClosesNextDay
                && endsAt <= startsAt)
            {
                return MenuOperationResult.Failure("Special end time must be later than the start time unless it closes the next day.");
            }

            normalizedSpecial = special with
            {
                DaysOfWeek = special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
                    ? normalizedSpecialDays
                    : Array.Empty<DayOfWeek>(),
                StartDate = special.ScheduleKind == MenuItemSpecialScheduleKind.Dated
                    ? special.StartDate
                    : null,
                EndDate = special.ScheduleKind == MenuItemSpecialScheduleKind.Dated
                    ? special.EndDate
                    : null,
                Callout = normalizedCallout
            };
        }

        var itemId = await repository.UpsertItemAsync(
            request with
            {
                Name = normalizedName,
                Description = normalizedDescription,
                ImagePath = NormalizeOptionalValue(request.ImagePath),
                OfferStartsOn = request.OfferStartsOn,
                OfferEndsOn = request.OfferEndsOn,
                IsSeasonal = request.IsSeasonal,
                SeasonStartMonth = normalizedSeasonalWindow?.StartMonth,
                SeasonStartDay = normalizedSeasonalWindow?.StartDay,
                SeasonEndMonth = normalizedSeasonalWindow?.EndMonth,
                SeasonEndDay = normalizedSeasonalWindow?.EndDay,
                PriceVariants = normalizedPriceVariants,
                SectionAssignments = normalizedSectionAssignments,
                MenuTabs = request.UsesSectionVisibility ? Array.Empty<MenuTab>() : normalizedTabs,
                Special = normalizedSpecial
            },
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("save", "item", itemId, normalizedName), cancellationToken);

        return MenuOperationResult.Success(itemId);
    }

    public async Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Days.Count != 7 || request.Days.Select(day => day.DayOfWeek).Distinct().Count() != 7)
        {
            return MenuOperationResult.Failure("Service hours must include one row for each day of the week.");
        }

        foreach (var day in request.Days)
        {
            if (!day.IsAvailable)
            {
                continue;
            }

            if (day.OpensAt is null || day.ClosesAt is null)
            {
                return MenuOperationResult.Failure("Available service-hour rows must include both opening and closing times.");
            }
        }

        await repository.UpsertServiceWindowsAsync(request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("save", "service-hours", null, MenuPresentationRules.GetTabLabel(request.Tab)), cancellationToken);

        return MenuOperationResult.Success();
    }

    public async Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateSortOrderRequests(requests, "section");
        if (validationResult is not null)
        {
            return validationResult;
        }

        var snapshot = await repository.GetMenuManagementSnapshotAsync(cancellationToken);
        var knownIds = snapshot.Sections.Select(section => section.SectionId).ToHashSet();
        if (requests.Any(request => !knownIds.Contains(request.RecordId)))
        {
            return MenuOperationResult.Failure("One or more sections could not be found for reordering.");
        }

        await repository.ReorderSectionsAsync(requests, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(
            new MenuOperationLogEntry("reorder", "section", null, $"Updated sort order for {requests.Count} section(s)."),
            cancellationToken);

        return MenuOperationResult.Success();
    }

    public async Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateSortOrderRequests(requests, "menu item");
        if (validationResult is not null)
        {
            return validationResult;
        }

        if (requests.Any(request => request.ContextId is null))
        {
            return MenuOperationResult.Failure("Menu item reordering must target a specific section.");
        }

        var targetSectionId = requests[0].ContextId!.Value;
        if (requests.Any(request => request.ContextId != targetSectionId))
        {
            return MenuOperationResult.Failure("Menu item reordering can only be saved for one section at a time.");
        }

        var snapshot = await repository.GetMenuManagementSnapshotAsync(cancellationToken);
        var itemsInTargetSection = snapshot.Items
            .Where(item => item.SectionAssignments.Any(assignment => assignment.SectionId == targetSectionId))
            .Select(item => item.ItemId)
            .ToHashSet();
        if (requests.Any(request => !itemsInTargetSection.Contains(request.RecordId)))
        {
            return MenuOperationResult.Failure("One or more menu items could not be found for reordering.");
        }

        await repository.ReorderItemsAsync(requests, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(
            new MenuOperationLogEntry("reorder", "item", null, $"Updated sort order for {requests.Count} menu item(s)."),
            cancellationToken);

        return MenuOperationResult.Success();
    }

    public async Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var section = await repository.GetSectionReferenceAsync(sectionId, cancellationToken);
        if (section is null)
        {
            return MenuOperationResult.Failure("The selected section could not be found.");
        }

        if (await repository.SectionHasDependentsAsync(sectionId, cancellationToken))
        {
            return MenuOperationResult.Failure("Move or remove the items in this section before archiving it.");
        }

        await repository.ArchiveSectionAsync(sectionId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("archive", "section", sectionId, $"Archived section {sectionId}"), cancellationToken);

        return MenuOperationResult.Success(sectionId);
    }

    public async Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var section = await repository.GetSectionReferenceAsync(sectionId, cancellationToken);
        if (section is null)
        {
            return MenuOperationResult.Failure("The selected section could not be found.");
        }

        if (await repository.SectionHasDependentsAsync(sectionId, cancellationToken))
        {
            return MenuOperationResult.Failure("Move or remove the items in this section before deleting it.");
        }

        await repository.DeleteSectionAsync(sectionId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("delete", "section", sectionId, $"Deleted section {sectionId}"), cancellationToken);

        return MenuOperationResult.Success(sectionId);
    }

    public async Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetItemReferenceAsync(itemId, cancellationToken);
        if (item is null)
        {
            return MenuOperationResult.Failure("The selected menu item could not be found.");
        }

        await repository.ArchiveItemAsync(itemId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("archive", "item", itemId, item.Name), cancellationToken);

        return MenuOperationResult.Success(itemId);
    }

    public async Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetItemReferenceAsync(itemId, cancellationToken);
        if (item is null)
        {
            return MenuOperationResult.Failure("The selected menu item could not be found.");
        }

        await repository.DeleteItemAsync(itemId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("delete", "item", itemId, item.Name), cancellationToken);

        return MenuOperationResult.Success(itemId);
    }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static SeasonalWindow? NormalizeSeasonalWindow(
        int? seasonStartMonth,
        int? seasonStartDay,
        int? seasonEndMonth,
        int? seasonEndDay,
        out string? error)
    {
        error = null;
        var hasAnyValue = seasonStartMonth is not null
            || seasonStartDay is not null
            || seasonEndMonth is not null
            || seasonEndDay is not null;

        if (!hasAnyValue)
        {
            return null;
        }

        if (seasonStartMonth is null || seasonEndMonth is null)
        {
            error = "Recurring seasonal items need both a start month and an end month.";
            return null;
        }

        if (seasonStartMonth is < 1 or > 12 || seasonEndMonth is < 1 or > 12)
        {
            error = "Seasonal months must be valid month numbers.";
            return null;
        }

        if (seasonStartDay is < 1 or > 31 || seasonEndDay is < 1 or > 31)
        {
            error = "Seasonal day values must be between 1 and 31.";
            return null;
        }

        return new SeasonalWindow(seasonStartMonth.Value, seasonStartDay, seasonEndMonth.Value, seasonEndDay);
    }

    private static MenuTab[] NormalizeSectionTabs(MenuFamily family, IReadOnlyList<MenuTab> requestedTabs) =>
        requestedTabs
            .Distinct()
            .Where(tab => family == MenuFamily.Drink ? tab == MenuTab.Drinks : tab != MenuTab.Drinks)
            .OrderBy(tab => tab)
            .ToArray();

    private static bool IsSectionDescendant(
        IReadOnlyList<MenuSectionRecord> sections,
        Guid candidateParentId,
        Guid currentSectionId)
    {
        var parentLookup = sections.ToDictionary(section => section.SectionId, section => section.ParentSectionId);
        var currentParentId = candidateParentId;

        while (parentLookup.TryGetValue(currentParentId, out var nextParentId))
        {
            if (currentParentId == currentSectionId)
            {
                return true;
            }

            if (nextParentId is null)
            {
                break;
            }

            currentParentId = nextParentId.Value;
        }

        return currentParentId == currentSectionId;
    }

    private static MenuOperationResult? ValidateSortOrderRequests(
        IReadOnlyList<SaveMenuSortOrderRequest> requests,
        string recordLabel)
    {
        if (requests.Count == 0)
        {
            return MenuOperationResult.Failure($"Add at least one {recordLabel} before saving sort-order changes.");
        }

        if (requests.Select(request => request.RecordId).Distinct().Count() != requests.Count)
        {
            return MenuOperationResult.Failure($"Each {recordLabel} can only appear once in a sort-order update.");
        }

        if (requests.Any(request => request.SortOrder <= 0))
        {
            return MenuOperationResult.Failure("Sort orders must be positive whole numbers.");
        }

        if (requests.Select(request => request.SortOrder).Distinct().Count() != requests.Count)
        {
            return MenuOperationResult.Failure($"Each {recordLabel} sort order must be unique.");
        }

        return null;
    }

    private sealed record SeasonalWindow(int StartMonth, int? StartDay, int EndMonth, int? EndDay);
}
