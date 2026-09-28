using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracker.App.Common.Constants;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data.Configurations;

public class DailyPlanConfiguration : IEntityTypeConfiguration<DailyPlan>
{
    public void Configure(EntityTypeBuilder<DailyPlan> builder)
    {
        builder.ToTable(DatabaseConstants.Tables.DailyPlans);

        builder.HasKey(d => d.Id);

        // A user can only have one daily plan per date
        builder.HasIndex(d => new { d.UserId, d.PlanDate })
            .IsUnique();

        builder.HasMany(d => d.Items)
            .WithOne(i => i.DailyPlan)
            .HasForeignKey(i => i.DailyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
