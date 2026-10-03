namespace Tracker.App.Common.Entities;

/// <summary>
/// Base class for all entities with a GUID primary key and audit timestamps.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Base class for entities that strictly belong to a specific user (multi-tenancy isolation).
/// </summary>
public abstract class UserOwnedEntity : BaseEntity
{
    public Guid UserId { get; set; }
}
