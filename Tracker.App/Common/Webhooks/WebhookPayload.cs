namespace Tracker.App.Common.Webhooks;

/// <summary>
/// Standard webhook delivery payload envelope.
/// Follows industry conventions (Stripe, GitHub) for reliable consumer parsing.
/// </summary>
public class WebhookPayload<T>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Event { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public T Data { get; set; } = default!;
}
