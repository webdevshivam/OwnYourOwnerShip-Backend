using Tracker.App.Common.Entities;

namespace Tracker.App.Data.Entities;

/// <summary>
/// Cross-cutting label for tasks (e.g. #urgent, #deep-work).
/// </summary>
public class Tag : UserOwnedEntity
{
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;

    // Many-to-Many Navigation via TaskTag join entity
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}
