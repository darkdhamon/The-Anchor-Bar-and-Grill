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
        builder.Property(section => section.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(section => section.Callout).HasMaxLength(200);
        builder.Property(section => section.Family).IsRequired();
        builder.Property(section => section.SortOrder).IsRequired();
        builder.Property(section => section.IsVisibleToGuests).IsRequired();
        builder.Property(section => section.IsArchived).IsRequired();
        builder.HasIndex(section => section.NormalizedName).IsUnique();
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
        builder.Property(item => item.NormalizedName).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.ImagePath).HasMaxLength(300);
        builder.Property(item => item.OfferStartsOn).HasColumnType("date");
        builder.Property(item => item.OfferEndsOn).HasColumnType("date");
        builder.Property(item => item.UsesSectionVisibility).IsRequired();
        builder.HasIndex(item => item.NormalizedName).IsUnique();
        builder.HasOne(item => item.Special)
            .WithOne(special => special.Item)
            .HasForeignKey<MenuItemSpecialEntity>(special => special.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
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

public sealed class MenuItemSectionAssignmentEntityConfiguration : IEntityTypeConfiguration<MenuItemSectionAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<MenuItemSectionAssignmentEntity> builder)
    {
        builder.ToTable("MenuItemSectionAssignments");
        builder.HasKey(assignment => new { assignment.MenuItemId, assignment.MenuSectionId });
        builder.Property(assignment => assignment.SortOrder).IsRequired();
        builder.HasOne(assignment => assignment.Item)
            .WithMany(item => item.SectionAssignments)
            .HasForeignKey(assignment => assignment.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(assignment => assignment.Section)
            .WithMany(section => section.ItemAssignments)
            .HasForeignKey(assignment => assignment.MenuSectionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasData(MenuSeedData.ItemSectionAssignments);
    }
}

public sealed class MenuItemTabEntityConfiguration : IEntityTypeConfiguration<MenuItemTabEntity>
{
    public void Configure(EntityTypeBuilder<MenuItemTabEntity> builder)
    {
        builder.ToTable("MenuItemTabs");
        builder.HasKey(link => new { link.MenuItemId, link.Tab });
        builder.HasOne(link => link.Item)
            .WithMany(item => item.MenuTabs)
            .HasForeignKey(link => link.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasData(MenuSeedData.ItemMenuTabs);
    }
}

public sealed class MenuSectionTabEntityConfiguration : IEntityTypeConfiguration<MenuSectionTabEntity>
{
    public void Configure(EntityTypeBuilder<MenuSectionTabEntity> builder)
    {
        builder.ToTable("MenuSectionTabs");
        builder.HasKey(link => new { link.MenuSectionId, link.Tab });
        builder.HasOne(link => link.Section)
            .WithMany(section => section.MenuTabs)
            .HasForeignKey(link => link.MenuSectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasData(MenuSeedData.SectionMenuTabs);
    }
}

public sealed class MenuItemSpecialEntityConfiguration : IEntityTypeConfiguration<MenuItemSpecialEntity>
{
    public void Configure(EntityTypeBuilder<MenuItemSpecialEntity> builder)
    {
        builder.ToTable("MenuItemSpecials");
        builder.HasKey(special => special.MenuItemId);
        builder.Property(special => special.ScheduleKind).IsRequired();
        builder.Property(special => special.StartDate).HasColumnType("date");
        builder.Property(special => special.EndDate).HasColumnType("date");
        builder.Property(special => special.StartsAt).HasColumnType("time");
        builder.Property(special => special.EndsAt).HasColumnType("time");
        builder.Property(special => special.Callout).HasMaxLength(100);
        builder.HasData(MenuSeedData.Specials);
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
