namespace Anchor.Domain.Menu;

public sealed class MenuManagementService(
    IMenuManagementRepository repository,
    IMenuOperationLogSink logSink) : IMenuManagementService
{
    public async Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default)
    {
        MenuSectionReferenceRecord? existingSection = null;
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

        if (existingSection is not null
            && existingSection.Family != request.Family
            && await repository.SectionHasDependentsAsync(existingSection.SectionId, cancellationToken))
        {
            return MenuOperationResult.Failure("Move or remove the section's items before changing it from food to drink or vice versa.");
        }

        var savedSectionId = await repository.UpsertSectionAsync(
            request with { Name = normalizedName },
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("save", "section", savedSectionId, normalizedName), cancellationToken);

        return MenuOperationResult.Success(savedSectionId);
    }

    public async Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default)
    {
        var section = await repository.GetSectionReferenceAsync(request.SectionId, cancellationToken);
        if (section is null)
        {
            return MenuOperationResult.Failure("Select a valid section before saving the menu item.");
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

        var normalizedDescription = request.Description.Trim();
        if (string.IsNullOrWhiteSpace(normalizedDescription))
        {
            return MenuOperationResult.Failure("Menu item description is required.");
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

        var normalizedTabs = request.FoodTabs
            .Distinct()
            .OrderBy(tab => tab)
            .ToArray();

        if (section.Family == MenuFamily.Food)
        {
            if (normalizedTabs.Length == 0)
            {
                return MenuOperationResult.Failure("Food items must appear on at least one of Breakfast, Lunch, or Dinner.");
            }

            if (normalizedTabs.Any(tab => tab == MenuTab.Drinks))
            {
                return MenuOperationResult.Failure("Food items cannot be assigned to the Drinks tab.");
            }
        }
        else if (normalizedTabs.Length > 0)
        {
            return MenuOperationResult.Failure("Drink items cannot be assigned to Breakfast, Lunch, or Dinner.");
        }

        if (request.Special is null
            && request.OfferStartsOn is not null
            && request.OfferEndsOn is not null
            && request.OfferEndsOn < request.OfferStartsOn)
        {
            return MenuOperationResult.Failure("Offer end date cannot be earlier than the offer start date.");
        }

        SaveMenuItemSpecialRequest? normalizedSpecial = null;
        if (request.Special is { } special)
        {
            var normalizedCallout = NormalizeOptionalValue(special.Callout);
            if (normalizedCallout is { Length: > 100 })
            {
                return MenuOperationResult.Failure("Special callouts cannot be longer than 100 characters.");
            }

            if (special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring && special.DayOfWeek is null)
            {
                return MenuOperationResult.Failure("Choose a weekday for recurring specials.");
            }

            if (special.EndDate is not null && special.EndDate < special.StartDate)
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
                DayOfWeek = special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
                    ? special.DayOfWeek
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
                OfferStartsOn = normalizedSpecial is null ? request.OfferStartsOn : null,
                OfferEndsOn = normalizedSpecial is null ? request.OfferEndsOn : null,
                IsSeasonal = normalizedSpecial is null && request.IsSeasonal,
                PriceVariants = normalizedPriceVariants,
                FoodTabs = normalizedTabs,
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

        var snapshot = await repository.GetMenuManagementSnapshotAsync(cancellationToken);
        var knownIds = snapshot.Items.Select(item => item.ItemId).ToHashSet();
        if (requests.Any(request => !knownIds.Contains(request.RecordId)))
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
}
