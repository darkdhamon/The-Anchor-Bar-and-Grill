using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Anchor.Infrastructure.Data.Events;
using Anchor.Infrastructure.Data.Menu;
using Anchor.Infrastructure.Data.Publicity;

namespace Anchor.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<EventEntity> Events => Set<EventEntity>();

    public DbSet<MenuSectionEntity> MenuSections => Set<MenuSectionEntity>();

    public DbSet<MenuItemEntity> MenuItems => Set<MenuItemEntity>();

    public DbSet<MenuItemPriceVariantEntity> MenuItemPriceVariants => Set<MenuItemPriceVariantEntity>();

    public DbSet<MenuItemSectionAssignmentEntity> MenuItemSectionAssignments => Set<MenuItemSectionAssignmentEntity>();

    public DbSet<MenuItemTabEntity> MenuItemTabs => Set<MenuItemTabEntity>();

    public DbSet<MenuItemSpecialEntity> MenuItemSpecials => Set<MenuItemSpecialEntity>();

    public DbSet<MenuItemSpecialDayEntity> MenuItemSpecialDays => Set<MenuItemSpecialDayEntity>();

    public DbSet<MenuSectionTabEntity> MenuSectionTabs => Set<MenuSectionTabEntity>();

    public DbSet<MenuServiceWindowEntity> MenuServiceWindows => Set<MenuServiceWindowEntity>();

    public DbSet<HomepagePublicityEntity> HomepagePublicity => Set<HomepagePublicityEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

