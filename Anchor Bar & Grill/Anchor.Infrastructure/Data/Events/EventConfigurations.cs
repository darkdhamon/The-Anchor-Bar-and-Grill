using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventEntityConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("Events");
        builder.HasKey(item => item.EventId);
        builder.Property(item => item.Title).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Summary).HasMaxLength(300).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.PromoBadge).HasMaxLength(80);
        builder.Property(item => item.ImagePath).HasMaxLength(300);
        builder.Property(item => item.StartsOn).HasColumnType("date");
        builder.Property(item => item.StartsAt).HasColumnType("time").IsRequired();
        builder.Property(item => item.EndsAt).HasColumnType("time");
        builder.Property(item => item.EndsNextDay).IsRequired();
        builder.Property(item => item.TimingNotes).HasMaxLength(300);
        builder.Property(item => item.SortOrder).IsRequired();
        builder.Property(item => item.PublicationState).IsRequired();
        builder.Property(item => item.RecurrencePattern).IsRequired();
        builder.Property(item => item.RecurrenceInterval).IsRequired();
        builder.Property(item => item.RecursUntil).HasColumnType("date");
        builder.HasIndex(item => new { item.PublicationState, item.StartsOn });
    }
}
