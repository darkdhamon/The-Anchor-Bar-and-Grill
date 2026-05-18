using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Anchor.Infrastructure.Data.Menu;

namespace Anchor.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<MenuSectionEntity> MenuSections => Set<MenuSectionEntity>();

    public DbSet<MenuItemEntity> MenuItems => Set<MenuItemEntity>();

    public DbSet<MenuItemPriceVariantEntity> MenuItemPriceVariants => Set<MenuItemPriceVariantEntity>();

    public DbSet<MenuItemTabEntity> MenuItemTabs => Set<MenuItemTabEntity>();

    public DbSet<RecurringSpecialEntity> RecurringSpecials => Set<RecurringSpecialEntity>();

    public DbSet<MenuServiceWindowEntity> MenuServiceWindows => Set<MenuServiceWindowEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

