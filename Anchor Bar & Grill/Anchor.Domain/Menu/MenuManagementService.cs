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
            return MenuOperationResult.Failure("Move or remove the section's items and specials before changing it from food to drink or vice versa.");
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

        if (request.OfferStartsOn is not null && request.OfferEndsOn is not null && request.OfferEndsOn < request.OfferStartsOn)
        {
            return MenuOperationResult.Failure("Offer end date cannot be earlier than the offer start date.");
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

        var itemId = await repository.UpsertItemAsync(
            request with
            {
                Name = normalizedName,
                Description = normalizedDescription,
                ImagePath = NormalizeOptionalValue(request.ImagePath),
                PriceVariants = normalizedPriceVariants,
                FoodTabs = normalizedTabs
            },
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("save", "item", itemId, normalizedName), cancellationToken);

        return MenuOperationResult.Success(itemId);
    }

    public async Task<MenuOperationResult> SaveRecurringSpecialAsync(SaveRecurringSpecialRequest request, CancellationToken cancellationToken = default)
    {
        var section = await repository.GetSectionReferenceAsync(request.SectionId, cancellationToken);
        if (section is null)
        {
            return MenuOperationResult.Failure("Select a valid section before saving the recurring special.");
        }

        var expectedFamily = request.Tab == MenuTab.Drinks ? MenuFamily.Drink : MenuFamily.Food;
        if (section.Family != expectedFamily)
        {
            return MenuOperationResult.Failure("Recurring specials must stay inside a section that matches the selected menu tab.");
        }

        var normalizedTitle = request.Title.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return MenuOperationResult.Failure("Recurring special title is required.");
        }

        var normalizedDescription = request.Description.Trim();
        if (string.IsNullOrWhiteSpace(normalizedDescription))
        {
            return MenuOperationResult.Failure("Recurring special description is required.");
        }

        var normalizedTimeNote = request.TimeNote.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTimeNote))
        {
            return MenuOperationResult.Failure("Recurring special time note is required.");
        }

        if (request.SpecialId is { } existingSpecialId)
        {
            var snapshot = await repository.GetMenuManagementSnapshotAsync(cancellationToken);
            if (!snapshot.Specials.Any(special => special.SpecialId == existingSpecialId))
            {
                return MenuOperationResult.Failure("The selected recurring special could not be found.");
            }
        }

        if (request.LinkedMenuItemId is { } linkedItemId)
        {
            var linkedItem = await repository.GetItemReferenceAsync(linkedItemId, cancellationToken);
            if (linkedItem is null)
            {
                return MenuOperationResult.Failure("The featured menu item could not be found.");
            }

            if (linkedItem.SectionId != request.SectionId)
            {
                return MenuOperationResult.Failure("Featured menu items must stay in the same section as the recurring special.");
            }

            if (linkedItem.Family != expectedFamily)
            {
                return MenuOperationResult.Failure("Featured menu items must match the family of the recurring special.");
            }

            if (request.Tab != MenuTab.Drinks && !linkedItem.FoodTabs.Contains(request.Tab))
            {
                return MenuOperationResult.Failure("Featured food items must appear on the same meal tab as the recurring special.");
            }
        }

        var specialId = await repository.UpsertRecurringSpecialAsync(
            request with
            {
                Title = normalizedTitle,
                Description = normalizedDescription,
                TimeNote = normalizedTimeNote,
                PriceNote = NormalizeOptionalValue(request.PriceNote)
            },
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("save", "recurring-special", specialId, normalizedTitle), cancellationToken);

        return MenuOperationResult.Success(specialId);
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

    public async Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var section = await repository.GetSectionReferenceAsync(sectionId, cancellationToken);
        if (section is null)
        {
            return MenuOperationResult.Failure("The selected section could not be found.");
        }

        if (await repository.SectionHasDependentsAsync(sectionId, cancellationToken))
        {
            return MenuOperationResult.Failure("Move or remove the items and specials in this section before archiving it.");
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
            return MenuOperationResult.Failure("Move or remove the items and specials in this section before deleting it.");
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

        if (await repository.ItemHasLinkedSpecialsAsync(itemId, cancellationToken))
        {
            return MenuOperationResult.Failure("Remove or update recurring specials that feature this item before deleting it.");
        }

        await repository.DeleteItemAsync(itemId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("delete", "item", itemId, item.Name), cancellationToken);

        return MenuOperationResult.Success(itemId);
    }

    public async Task<MenuOperationResult> ArchiveRecurringSpecialAsync(Guid specialId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.GetMenuManagementSnapshotAsync(cancellationToken);
        var special = snapshot.Specials.SingleOrDefault(item => item.SpecialId == specialId);
        if (special is null)
        {
            return MenuOperationResult.Failure("The selected recurring special could not be found.");
        }

        await repository.ArchiveRecurringSpecialAsync(specialId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("archive", "recurring-special", specialId, special.Title), cancellationToken);

        return MenuOperationResult.Success(specialId);
    }

    public async Task<MenuOperationResult> DeleteRecurringSpecialAsync(Guid specialId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.GetMenuManagementSnapshotAsync(cancellationToken);
        var special = snapshot.Specials.SingleOrDefault(item => item.SpecialId == specialId);
        if (special is null)
        {
            return MenuOperationResult.Failure("The selected recurring special could not be found.");
        }

        await repository.DeleteRecurringSpecialAsync(specialId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(new MenuOperationLogEntry("delete", "recurring-special", specialId, special.Title), cancellationToken);

        return MenuOperationResult.Success(specialId);
    }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
