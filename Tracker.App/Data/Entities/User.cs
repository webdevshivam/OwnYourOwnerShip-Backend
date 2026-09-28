using Tracker.App.Common.Entities;

namespace Tracker.App.Data.Entities;

/// <summary>
/// Represents an application user account.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;

    // Brute-force & security tracking
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTimeOffset? LockoutUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    public ICollection<DailyPlan> DailyPlans { get; set; } = new List<DailyPlan>();
    public ICollection<RecurrenceRule> RecurrenceRules { get; set; } = new List<RecurrenceRule>();
    public ICollection<TaskActivityLog> TaskActivityLogs { get; set; } = new List<TaskActivityLog>();
}
