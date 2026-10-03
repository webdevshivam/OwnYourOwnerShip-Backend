using Tracker.App.Common.Entities;
using Tracker.App.Common.Enums;

namespace Tracker.App.Data.Entities;

/// <summary>
/// Core aggregate entity representing a user's task or subtask.
/// </summary>
public class TaskItem : UserOwnedEntity
{
    public User User { get; set; } = null!;

    // Optional project category
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    // Self-referencing relationship for subtasks
    public Guid? ParentTaskId { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> Subtasks { get; set; } = new List<TaskItem>();

    // Optional link to recurring rule origin
    public Guid? RecurrenceRuleId { get; set; }
    public RecurrenceRule? RecurrenceRule { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskLifecycleStatus Status { get; set; } = TaskLifecycleStatus.Created;

    public DateTimeOffset? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int? ActualMinutes { get; set; }

    // Fast counters for recommendation heuristics (eliminates expensive log aggregations)
    public int PostponeCount { get; set; } = 0;
    public int AttemptCount { get; set; } = 0;

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Concurrency token to protect against lost updates.
    /// </summary>
    public int Version { get; set; } = 1;

    // Navigation collections
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    public ICollection<TaskActivityLog> ActivityLogs { get; set; } = new List<TaskActivityLog>();
    public ICollection<DailyPlanItem> DailyPlanItems { get; set; } = new List<DailyPlanItem>();
}
