namespace Tracker.App.Common.Enums;

/// <summary>
/// Event types recorded in the task behavioral activity log.
/// Stored as SMALLINT in PostgreSQL (1 byte).
/// </summary>
public enum ActivityEventType : byte
{
    Created = 1,
    Started = 2,
    Completed = 3,
    Postponed = 4,
    Cancelled = 5,
    PriorityChanged = 6,
    DueDateChanged = 7
}
