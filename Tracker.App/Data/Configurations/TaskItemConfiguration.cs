using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracker.App.Common.Constants;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable(DatabaseConstants.Tables.TaskItems);

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(ValidationConstants.TaskTitleMaxLength);

        builder.Property(t => t.Priority)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(t => t.PostponeCount)
            .HasDefaultValue(0);

        builder.Property(t => t.AttemptCount)
            .HasDefaultValue(0);

        // Optimistic concurrency protection
        builder.Property(t => t.Version)
            .IsConcurrencyToken();

        // If Project is deleted, keep the Task (SET NULL)
        builder.HasOne(t => t.Project)
            .WithMany(p => p.TaskItems)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Subtasks: If parent task is deleted, delete subtasks (CASCADE)
        builder.HasOne(t => t.ParentTask)
            .WithMany(p => p.Subtasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Recurrence Rule origin
        builder.HasOne(t => t.RecurrenceRule)
            .WithMany(r => r.GeneratedTasks)
            .HasForeignKey(t => t.RecurrenceRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for high-frequency queries
        builder.HasIndex(t => new { t.UserId, t.Status, t.DueDate })
            .HasDatabaseName("idx_tasks_user_status_due");

        builder.HasIndex(t => new { t.UserId, t.ProjectId })
            .HasDatabaseName("idx_tasks_user_project");

        builder.HasIndex(t => t.ParentTaskId)
            .HasDatabaseName("idx_tasks_parent");
    }
}
