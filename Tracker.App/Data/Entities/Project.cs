using Tracker.App.Common.Entities;

namespace Tracker.App.Data.Entities;

/// <summary>
/// Organizes tasks into high-level categories or projects (e.g. "Work", "Fitness").
/// </summary>
public class Project : UserOwnedEntity
{
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public bool IsArchived { get; set; } = false;

    // Navigation
    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
}
