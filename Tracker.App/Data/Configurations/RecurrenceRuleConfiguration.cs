using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracker.App.Common.Constants;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data.Configurations;

public class RecurrenceRuleConfiguration : IEntityTypeConfiguration<RecurrenceRule>
{
    public void Configure(EntityTypeBuilder<RecurrenceRule> builder)
    {
        builder.ToTable(DatabaseConstants.Tables.RecurrenceRules);

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(ValidationConstants.RecurrenceTitleMaxLength);

        builder.Property(r => r.DaysOfWeekMask)
            .HasMaxLength(ValidationConstants.DaysOfWeekMaskMaxLength);

        builder.Property(r => r.IntervalValue)
            .HasDefaultValue(1);

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(r => new { r.UserId, r.IsActive });
    }
}
