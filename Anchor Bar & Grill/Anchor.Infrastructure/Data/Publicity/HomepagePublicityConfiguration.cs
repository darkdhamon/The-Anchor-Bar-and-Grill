using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Anchor.Infrastructure.Data.Publicity;

public sealed class HomepagePublicityConfiguration : IEntityTypeConfiguration<HomepagePublicityEntity>
{
    public void Configure(EntityTypeBuilder<HomepagePublicityEntity> builder)
    {
        builder.ToTable("HomepagePublicity");
        builder.HasKey(item => item.HomepagePublicityId);
        builder.Property(item => item.HomepagePublicityId).ValueGeneratedNever();
        builder.Property(item => item.DraftEyebrow).HasMaxLength(80);
        builder.Property(item => item.DraftHeadline).HasMaxLength(120);
        builder.Property(item => item.DraftSummary).HasMaxLength(1000);
        builder.Property(item => item.PublishedEyebrow).HasMaxLength(80);
        builder.Property(item => item.PublishedHeadline).HasMaxLength(120);
        builder.Property(item => item.PublishedSummary).HasMaxLength(1000);
    }
}
