namespace Tracker.App.Common.Webhooks;

/// <summary>
/// Service interface for dispatching signed webhooks and verifying incoming signatures.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Enqueues a webhook delivery as a background task (via Hangfire) to avoid blocking the main request thread.
    /// Hangfire automatically retries delivery with exponential backoff if the receiver fails.
    /// </summary>
    void EnqueueWebhook<T>(string targetUrl, string secretKey, string eventType, T data);

    /// <summary>
    /// Directly delivers a signed webhook HTTP POST request to the recipient URL.
    /// </summary>
    Task<bool> DeliverAsync(string targetUrl, string secretKey, string eventType, string payloadJson);

    /// <summary>
    /// Verifies the HMAC-SHA256 signature of an incoming webhook payload to ensure authenticity.
    /// </summary>
    bool VerifySignature(string payloadJson, string receivedSignature, string secretKey);
}
