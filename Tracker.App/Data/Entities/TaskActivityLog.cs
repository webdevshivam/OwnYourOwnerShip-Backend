using Tracker.App.Common.Enums;

namespace Tracker.App.Data.Entities;

/// <summary>
/// Immutable, append-only activity event timeline.
/// Captures user behavior patterns used by the recommendation engine.
/// </summary>
public class TaskActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public ActivityEventType EventType { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Extracted day of week (0 = Sunday ... 6 = Saturday) for fast habit queries.
    /// </summary>
    public byte DayOfWeek { get; set; }

    /// <summary>
    /// Extracted time of day (e.g. 09:30:00) for time-slot productivity analysis.
    /// </summary>
    public TimeOnly TimeOfDay { get; set; }

    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Optional contextual JSON payload (e.g. previous due date, postponement reason).
    /// Mapped directly to PostgreSQL JSONB for indexing and flexibility.
    /// </summary>
    public string? MetadataJson { get; set; }
}
