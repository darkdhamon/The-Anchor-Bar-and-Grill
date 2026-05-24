using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuSectionTabEntity
{
    public Guid MenuSectionId { get; set; }

    public MenuSectionEntity Section { get; set; } = null!;

    public MenuTab Tab { get; set; }
}
