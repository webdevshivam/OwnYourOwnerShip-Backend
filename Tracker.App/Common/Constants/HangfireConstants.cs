namespace Tracker.App.Common.Constants;

/// <summary>
/// Centralized constants for Hangfire background processing, dashboard routes, and cron expressions.
/// </summary>
public static class HangfireConstants
{
    // ==========================================
    // Dashboard Routes & Titles
    // ==========================================
    public const string DashboardPath = "/hangfire";
    public const string DashboardTitle = "OwnYourOwnership Background Jobs";

    // ==========================================
    // Job Names & Identifiers
    // ==========================================
    public const string MidnightRecurringTasksJobId = "midnight-recurrence-generator";
    public const string WeeklyTokenCleanupJobId = "weekly-expired-token-cleanup";
    public const string WebhookDispatchQueue = "webhooks";

    // ==========================================
    // Standard Cron Expressions
    // ==========================================
    /// <summary>
    /// Executes every day at 00:01 AM (Midnight + 1 minute).
    /// </summary>
    public const string DailyMidnightCron = "1 0 * * *";

    /// <summary>
    /// Executes every Sunday at 02:00 AM.
    /// </summary>
    public const string WeeklySunday2AmCron = "0 2 * * 0";

    // ==========================================
    // Schema & Database Configuration
    // ==========================================
    public const string HangfireSchemaName = "hangfire";
    public const int DefaultWorkerCount = 5;
}
