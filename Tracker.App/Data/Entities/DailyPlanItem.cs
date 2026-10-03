using Tracker.App.Common.Enums;

namespace Tracker.App.Data.Entities;

/// <summary>
/// An individual task placed on a daily plan.
/// Tracks human-in-the-loop decisions (Accepted, Rejected, Carried Over)
/// to provide learning feedback to the recommendation model.
/// </summary>
public class DailyPlanItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DailyPlanId { get; set; }
    public DailyPlan DailyPlan { get; set; } = null!;

    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public PlanSourceType SourceType { get; set; } = PlanSourceType.UserSelected;
    public UserDecision UserDecision { get; set; } = UserDecision.Pending;

    /// <summary>
    /// Calculated score (0.00 - 100.00) assigned by recommendation engine.
    /// </summary>
    public decimal? RecommendationScore { get; set; }

    /// <summary>
    /// Explainable reason shown in UI (e.g. "Due tomorrow and matches your morning routine").
    /// </summary>
    public string? RecommendationReason { get; set; }

    public int SortOrder { get; set; } = 0;
    public DateTimeOffset? DecisionAt { get; set; }
}
