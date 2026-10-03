namespace Tracker.App.Common.Enums;

/// <summary>
/// User decision on a daily plan task or recommendation.
/// Used directly to train and refine the recommendation engine.
/// </summary>
public enum UserDecision : byte
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    AutoCarriedOver = 4
}
