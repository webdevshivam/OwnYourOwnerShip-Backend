namespace Tracker.App.Common.Constants;

/// <summary>
/// Centralized database table names, column constraints, and default database values.
/// Eliminates hardcoded strings in EF Core mapping configurations.
/// </summary>
public static class DatabaseConstants
{
    // ==========================================
    // Table Names (PostgreSQL snake_case convention)
    // ==========================================
    public static class Tables
    {
        public const string Users = "users";
        public const string RefreshTokens = "refresh_tokens";
        public const string Projects = "projects";
        public const string Tags = "tags";
        public const string TaskItems = "task_items";
        public const string TaskTags = "task_tags";
        public const string RecurrenceRules = "recurrence_rules";
        public const string TaskActivityLogs = "task_activity_logs";
        public const string DailyPlans = "daily_plans";
        public const string DailyPlanItems = "daily_plan_items";
    }

    // ==========================================
    // Default Values
    // ==========================================
    public static class Defaults
    {
        public const string ProjectDefaultColorHex = "#6B7280";
        public const string TagDefaultColorHex = "#9CA3AF";
        public const string JsonEmptyObject = "{}";
    }

    // ==========================================
    // PostgreSQL Column Types
    // ==========================================
    public static class ColumnTypes
    {
        public const string Jsonb = "jsonb";
        public const string TimestampWithTimeZone = "timestamp with time zone";
        public const string Date = "date";
        public const string Time = "time without time zone";
    }
}
