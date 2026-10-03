using Tracker.App.Common.Entities;

namespace Tracker.App.Data.Entities;

/// <summary>
/// Daily planning container for a user's calendar day.
/// Exactly one plan exists per user per date.
/// </summary>
public class DailyPlan : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly PlanDate { get; set; }

    /// <summary>
    /// Total focused minutes available today (helps recommendation engine bound total load).
    /// </summary>
    public int? AvailableMinutes { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public ICollection<DailyPlanItem> Items { get; set; } = new List<DailyPlanItem>();
}
