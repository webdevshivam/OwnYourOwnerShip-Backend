namespace Tracker.App.Common.Enums;

/// <summary>
/// Frequency for repeating tasks.
/// Stored as SMALLINT in PostgreSQL (1 byte).
/// </summary>
public enum RecurrenceFrequency : byte
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Weekdays = 4
}
