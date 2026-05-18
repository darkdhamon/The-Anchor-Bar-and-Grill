using Anchor.Domain.Menu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuSectionEntityConfiguration : IEntityTypeConfiguration<MenuSectionEntity>
{
    public void Configure(EntityTypeBuilder<MenuSectionEntity> builder)
    {
        builder.ToTable("MenuSections");
        builder.HasKey(section => section.MenuSectionId);
        builder.Property(section => section.Name).HasMaxLength(100).IsRequired();
        builder.Property(section => section.Family).IsRequired();
        builder.Property(section => section.SortOrder).IsRequired();
        builder.Property(section => section.IsVisibleToGuests).IsRequired();
        builder.Property(section => section.IsArchived).IsRequired();
        builder.HasData(MenuSeedData.Sections);
    }
}

public sealed class MenuItemEntityConfiguration : IEntityTypeConfiguration<MenuItemEntity>
{
    public void Configure(EntityTypeBuilder<MenuItemEntity> builder)
    {
        builder.ToTable("MenuItems");
        builder.HasKey(item => item.MenuItemId);
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.ImagePath).HasMaxLength(300);
        builder.Property(item => item.OfferStartsOn).HasColumnType("date");
        builder.Property(item => item.OfferEndsOn).HasColumnType("date");
        builder.HasOne(item => item.Section)
            .WithMany(section => section.Items)
            .HasForeignKey(item => item.MenuSectionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasData(MenuSeedData.Items);
    }
}

public sealed class MenuItemPriceVariantEntityConfiguration : IEntityTypeConfiguration<MenuItemPriceVariantEntity>
{
    public void Configure(EntityTypeBuilder<MenuItemPriceVariantEntity> builder)
    {
        builder.ToTable("MenuItemPriceVariants");
        builder.HasKey(variant => variant.MenuItemPriceVariantId);
        builder.Property(variant => variant.Label).HasMaxLength(50).IsRequired();
        builder.Property(variant => variant.Amount).HasColumnType("decimal(10,2)");
        builder.HasOne(variant => variant.Item)
            .WithMany(item => item.PriceVariants)
            .HasForeignKey(variant => variant.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasData(MenuSeedData.PriceVariants);
    }
}

public sealed class MenuItemTabEntityConfiguration : IEntityTypeConfiguration<MenuItemTabEntity>
{
    public void Configure(EntityTypeBuilder<MenuItemTabEntity> builder)
    {
        builder.ToTable("MenuItemTabs");
        builder.HasKey(link => new { link.MenuItemId, link.Tab });
        builder.HasOne(link => link.Item)
            .WithMany(item => item.FoodTabs)
            .HasForeignKey(link => link.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasData(MenuSeedData.FoodItemTabs);
    }
}

public sealed class RecurringSpecialEntityConfiguration : IEntityTypeConfiguration<RecurringSpecialEntity>
{
    public void Configure(EntityTypeBuilder<RecurringSpecialEntity> builder)
    {
        builder.ToTable("RecurringSpecials");
        builder.HasKey(special => special.RecurringSpecialId);
        builder.Property(special => special.Title).HasMaxLength(150).IsRequired();
        builder.Property(special => special.Description).HasMaxLength(1000).IsRequired();
        builder.Property(special => special.TimeNote).HasMaxLength(100).IsRequired();
        builder.Property(special => special.PriceNote).HasMaxLength(100);
        builder.HasOne(special => special.Section)
            .WithMany(section => section.RecurringSpecials)
            .HasForeignKey(special => special.MenuSectionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(special => special.LinkedMenuItem)
            .WithMany(item => item.LinkedRecurringSpecials)
            .HasForeignKey(special => special.LinkedMenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasData(MenuSeedData.RecurringSpecials);
    }
}

public sealed class MenuServiceWindowEntityConfiguration : IEntityTypeConfiguration<MenuServiceWindowEntity>
{
    public void Configure(EntityTypeBuilder<MenuServiceWindowEntity> builder)
    {
        builder.ToTable("MenuServiceWindows");
        builder.HasKey(window => new { window.Tab, window.DayOfWeek });
        builder.Property(window => window.OpensAt).HasColumnType("time");
        builder.Property(window => window.ClosesAt).HasColumnType("time");
        builder.HasData(MenuSeedData.ServiceWindows);
    }
}
