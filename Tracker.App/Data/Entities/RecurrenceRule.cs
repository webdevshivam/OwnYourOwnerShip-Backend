using Tracker.App.Common.Entities;
using Tracker.App.Common.Enums;

namespace Tracker.App.Data.Entities;

/// <summary>
/// Schedule template that automatically spawns fresh TaskItem instances for recurring tasks.
/// </summary>
public class RecurrenceRule : UserOwnedEntity
{
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Daily;
    public int IntervalValue { get; set; } = 1;

    /// <summary>
    /// Comma-separated mask for weekly patterns (e.g. "MON,WED,FRI").
    /// </summary>
    public string? DaysOfWeekMask { get; set; }

    public TimeOnly? ScheduledTime { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;
    public DateOnly? LastGeneratedDate { get; set; }

    // Navigation
    public ICollection<TaskItem> GeneratedTasks { get; set; } = new List<TaskItem>();
}
