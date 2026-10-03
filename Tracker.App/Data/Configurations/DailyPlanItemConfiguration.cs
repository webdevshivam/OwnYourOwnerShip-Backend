using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracker.App.Common.Constants;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data.Configurations;

public class DailyPlanItemConfiguration : IEntityTypeConfiguration<DailyPlanItem>
{
    public void Configure(EntityTypeBuilder<DailyPlanItem> builder)
    {
        builder.ToTable(DatabaseConstants.Tables.DailyPlanItems);

        builder.HasKey(dpi => dpi.Id);

        builder.Property(dpi => dpi.SourceType)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(dpi => dpi.UserDecision)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(dpi => dpi.RecommendationScore)
            .HasPrecision(5, 2);

        builder.Property(dpi => dpi.RecommendationReason)
            .HasMaxLength(ValidationConstants.RecommendationReasonMaxLength);

        builder.Property(dpi => dpi.SortOrder)
            .HasDefaultValue(0);

        builder.HasOne(dpi => dpi.Task)
            .WithMany(t => t.DailyPlanItems)
            .HasForeignKey(dpi => dpi.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // A task cannot be duplicated within the same day's plan
        builder.HasIndex(dpi => new { dpi.DailyPlanId, dpi.TaskId })
            .IsUnique();

        builder.HasIndex(dpi => new { dpi.DailyPlanId, dpi.SortOrder });
    }
}
