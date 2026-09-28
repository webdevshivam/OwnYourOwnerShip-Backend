namespace Tracker.App.Common.Enums;

/// <summary>
/// Priority level for a task.
/// Stored as SMALLINT in PostgreSQL (1 byte).
/// </summary>
public enum TaskPriority : byte
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}
