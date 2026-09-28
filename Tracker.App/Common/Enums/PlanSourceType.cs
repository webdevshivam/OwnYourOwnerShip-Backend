namespace Tracker.App.Common.Enums;

/// <summary>
/// Source origin of a task in the daily plan.
/// </summary>
public enum PlanSourceType : byte
{
    UserSelected = 1,
    SystemRecommended = 2
}
