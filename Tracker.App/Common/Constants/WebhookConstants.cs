namespace Tracker.App.Common.Constants;

/// <summary>
/// Centralized constants for Webhook delivery, headers, event types, and HTTP parameters.
/// </summary>
public static class WebhookConstants
{
    // ==========================================
    // HTTP Headers (Industry Standard like Stripe/GitHub)
    // ==========================================
    public const string SignatureHeader = "X-Webhook-Signature";
    public const string EventHeader = "X-Webhook-Event";
    public const string TimestampHeader = "X-Webhook-Timestamp";

    // ==========================================
    // Supported Webhook Events
    // ==========================================
    public const string TaskCreatedEvent = "task.created";
    public const string TaskCompletedEvent = "task.completed";
    public const string TaskPostponedEvent = "task.postponed";
    public const string TaskCancelledEvent = "task.cancelled";
    public const string DailyPlanGeneratedEvent = "daily_plan.generated";

    // ==========================================
    // Operational Defaults
    // ==========================================
    public const int DefaultTimeoutSeconds = 10;
    public const int DefaultMaxRetries = 3;
    public const string HttpClientName = "WebhookHttpClient";
}
