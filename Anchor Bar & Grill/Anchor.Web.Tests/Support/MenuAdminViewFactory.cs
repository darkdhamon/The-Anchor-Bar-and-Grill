using Anchor.Domain.Menu;

namespace Anchor.Web.Tests.Support;

internal static class MenuAdminViewFactory
{
    public static MenuSectionAdminView Section(
        Guid sectionId,
        string name,
        MenuFamily family,
        IReadOnlyList<MenuTab> menuTabs,
        int sortOrder,
        bool isVisibleToGuests = true,
        bool isArchived = false,
        string? callout = null,
        Guid? parentSectionId = null,
        string? parentSectionName = null,
        IReadOnlyList<string>? statusLabels = null) =>
        new(
            sectionId,
            name,
            callout,
            family,
            parentSectionId,
            parentSectionName,
            menuTabs,
            sortOrder,
            isVisibleToGuests,
            isArchived,
            statusLabels ?? []);

    public static MenuItemSectionAssignmentView Assignment(Guid sectionId, string sectionName, int sortOrder = 1) =>
        new(sectionId, sectionName, sortOrder);

    public static MenuItemAdminView Item(
        Guid itemId,
        MenuFamily family,
        string name,
        string description,
        int sortOrder,
        IReadOnlyList<MenuItemSectionAssignmentView> sectionAssignments,
        IReadOnlyList<MenuTab> menuTabs,
        IReadOnlyList<MenuItemPriceVariantView> priceVariants,
        IReadOnlyList<string>? statusLabels = null,
        string? offerDateSummary = null,
        MenuItemSpecialAdminView? special = null,
        string? imagePath = null,
        bool isVisibleToGuests = true,
        bool isArchived = false,
        DateOnly? offerStartsOn = null,
        DateOnly? offerEndsOn = null,
        bool isSeasonal = false,
        bool usesSectionVisibility = false) =>
        new(
            itemId,
            family,
            name,
            description,
            imagePath,
            sortOrder,
            isVisibleToGuests,
            isArchived,
            offerStartsOn,
            offerEndsOn,
            isSeasonal,
            sectionAssignments,
            usesSectionVisibility,
            menuTabs,
            priceVariants,
            statusLabels ?? [],
            offerDateSummary,
            special);
}
