using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuItemTabEntity
{
    public Guid MenuItemId { get; set; }

    public MenuItemEntity Item { get; set; } = null!;

    public MenuTab Tab { get; set; }
}
