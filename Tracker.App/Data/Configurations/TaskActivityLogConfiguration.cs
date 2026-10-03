using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracker.App.Common.Constants;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data.Configurations;

public class TaskActivityLogConfiguration : IEntityTypeConfiguration<TaskActivityLog>
{
    public void Configure(EntityTypeBuilder<TaskActivityLog> builder)
    {
        builder.ToTable(DatabaseConstants.Tables.TaskActivityLogs);

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EventType)
            .HasConversion<byte>()
            .IsRequired();

        // Native PostgreSQL JSONB column for flexible metadata
        builder.Property(a => a.MetadataJson)
            .HasColumnType(DatabaseConstants.ColumnTypes.Jsonb);

        builder.HasOne(a => a.Task)
            .WithMany(t => t.ActivityLogs)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for chronological task audit trail
        builder.HasIndex(a => new { a.UserId, a.TaskId, a.Timestamp })
            .HasDatabaseName("idx_activity_user_task");

        // Index for AI / Recommendation feature extraction (hourly & weekday habit queries)
        builder.HasIndex(a => new { a.UserId, a.EventType, a.DayOfWeek, a.TimeOfDay })
            .HasDatabaseName("idx_activity_features");
    }
}
