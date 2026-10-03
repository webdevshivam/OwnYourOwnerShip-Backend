using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hangfire;
using Tracker.App.Common.Constants;

namespace Tracker.App.Common.Webhooks;

/// <summary>
/// Production-grade webhook delivery service.
/// Uses HMAC-SHA256 payload signing, constant-time verification,
/// and Hangfire background queues for resilient retries.
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(IHttpClientFactory httpClientFactory, ILogger<WebhookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Enqueues the webhook to be dispatched asynchronously via Hangfire.
    /// This prevents blocking HTTP responses and guarantees automatic retries if the receiver is down.
    /// </summary>
    public void EnqueueWebhook<T>(string targetUrl, string secretKey, string eventType, T data)
    {
        var payload = new WebhookPayload<T>
        {
            Event = eventType,
            Timestamp = DateTimeOffset.UtcNow,
            Data = data
        };

        string payloadJson = JsonSerializer.Serialize(payload);

        // Enqueue to Hangfire on the dedicated "webhooks" queue
        BackgroundJob.Enqueue<IWebhookService>(service =>
            service.DeliverAsync(targetUrl, secretKey, eventType, payloadJson));
    }

    /// <summary>
    /// Executes the actual HTTP POST request to deliver the webhook.
    /// </summary>
    [Queue(HangfireConstants.WebhookDispatchQueue)]
    public async Task<bool> DeliverAsync(string targetUrl, string secretKey, string eventType, string payloadJson)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(WebhookConstants.HttpClientName);

            // 1. Calculate HMAC-SHA256 signature
            string signature = ComputeHmacSignature(payloadJson, secretKey);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // 2. Build HTTP request with security headers
            using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            request.Headers.Add(WebhookConstants.SignatureHeader, signature);
            request.Headers.Add(WebhookConstants.EventHeader, eventType);
            request.Headers.Add(WebhookConstants.TimestampHeader, timestamp.ToString());

            // 3. Send request
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook {Event} successfully delivered to {Url}", eventType, targetUrl);
                return true;
            }

            _logger.LogWarning("Webhook delivery failed to {Url}. Status code: {StatusCode}", targetUrl, response.StatusCode);
            throw new HttpRequestException($"Webhook endpoint returned non-success code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception delivering webhook {Event} to {Url}", eventType, targetUrl);
            throw; // Rethrowing allows Hangfire to trigger automatic exponential backoff retries!
        }
    }

    /// <summary>
    /// Verifies the signature of an incoming webhook from an external service.
    /// </summary>
    public bool VerifySignature(string payloadJson, string receivedSignature, string secretKey)
    {
        if (string.IsNullOrWhiteSpace(payloadJson) ||
            string.IsNullOrWhiteSpace(receivedSignature) ||
            string.IsNullOrWhiteSpace(secretKey))
        {
            return false;
        }

        string expectedSignature = ComputeHmacSignature(payloadJson, secretKey);

        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        byte[] receivedBytes = Encoding.UTF8.GetBytes(receivedSignature);

        // Constant-time comparison to protect against side-channel timing attacks
        return CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
    }

    /// <summary>
    /// Computes the HMAC-SHA256 signature of a payload string using a shared secret key.
    /// </summary>
    private static string ComputeHmacSignature(string payload, string secretKey)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(payloadBytes);

        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
