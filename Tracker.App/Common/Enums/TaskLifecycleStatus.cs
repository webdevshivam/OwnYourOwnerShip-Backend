namespace Tracker.App.Common.Enums;

/// <summary>
/// Lifecycle status of a task item.
/// Named TaskLifecycleStatus to avoid naming collisions with System.Threading.Tasks.TaskStatus.
/// Stored as SMALLINT in PostgreSQL (1 byte).
/// </summary>
public enum TaskLifecycleStatus : byte
{
    Created = 1,
    Planned = 2,
    InProgress = 3,
    Completed = 4,
    Postponed = 5,
    Cancelled = 6,
    Skipped = 7
}
