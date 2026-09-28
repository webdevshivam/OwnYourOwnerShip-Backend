namespace Tracker.App.Data.Entities;

/// <summary>
/// Associative join entity connecting TaskItem and Tag (Many-to-Many).
/// Uses composite primary key (TaskId, TagId).
/// </summary>
public class TaskTag
{
    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
