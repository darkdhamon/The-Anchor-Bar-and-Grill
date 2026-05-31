using Anchor.Domain.Publicity;
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
        builder.Property(item => item.DraftEyebrow).HasMaxLength(HomepagePublicityConstraints.EyebrowMaxLength);
        builder.Property(item => item.DraftHeadline).HasMaxLength(HomepagePublicityConstraints.HeadlineMaxLength);
        builder.Property(item => item.DraftSummary).HasMaxLength(HomepagePublicityConstraints.SummaryMaxLength);
        builder.Property(item => item.PublishedEyebrow).HasMaxLength(HomepagePublicityConstraints.EyebrowMaxLength);
        builder.Property(item => item.PublishedHeadline).HasMaxLength(HomepagePublicityConstraints.HeadlineMaxLength);
        builder.Property(item => item.PublishedSummary).HasMaxLength(HomepagePublicityConstraints.SummaryMaxLength);
    }
}
