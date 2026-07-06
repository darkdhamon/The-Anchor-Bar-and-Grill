using Anchor.Domain.Menu;
using Anchor.Web.Components.Site;
using Bunit;

namespace Anchor.Web.Tests.Components.Site;

public sealed class MenuItemCardTests : BunitContext
{
    [Fact]
    public void Image_thumbnail_normalizes_relative_paths_and_renders_preview_modal_markup()
    {
        var item = new PublicMenuItemView(
            Guid.NewGuid(),
            "Cheese Curds",
            "Crisp white cheddar curds.",
            "images/menu/appetizers.svg",
            [new MenuItemPriceVariantView("Regular", 9m, 1)],
            [],
            null,
            null);

        var cut = Render<MenuItemCard>(parameters => parameters
            .Add(component => component.Item, item));

        var trigger = cut.Find(".image-preview__trigger");
        var modal = cut.Find(".image-preview-modal-backdrop");

        Assert.Equal("/images/menu/appetizers.svg", cut.Find(".menu-item__image").GetAttribute("src"));
        Assert.True(trigger.HasAttribute("data-image-preview-open"));
        Assert.Equal(modal.Id, trigger.GetAttribute("aria-controls"));
        Assert.Contains("data-image-preview-modal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.True(modal.HasAttribute("hidden"));
        Assert.Contains("/images/menu/appetizers.svg", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Items_without_images_do_not_render_preview_triggers()
    {
        var item = new PublicMenuItemView(
            Guid.NewGuid(),
            "Cheese Curds",
            "Crisp white cheddar curds.",
            null,
            [new MenuItemPriceVariantView("Regular", 9m, 1)],
            [],
            null,
            null);

        var cut = Render<MenuItemCard>(parameters => parameters
            .Add(component => component.Item, item));

        Assert.Empty(cut.FindAll(".image-preview__trigger"));
    }
}
