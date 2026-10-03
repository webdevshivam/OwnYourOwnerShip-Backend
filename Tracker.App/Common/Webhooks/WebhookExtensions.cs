using Tracker.App.Common.Constants;

namespace Tracker.App.Common.Webhooks;

/// <summary>
/// Dependency injection extensions for registering Webhook services.
/// </summary>
public static class WebhookExtensions
{
    public static IServiceCollection AddWebhookServices(this IServiceCollection services)
    {
        // Configure dedicated resilient HttpClient for Webhook delivery
        services.AddHttpClient(WebhookConstants.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(WebhookConstants.DefaultTimeoutSeconds);
        });

        services.AddScoped<IWebhookService, WebhookService>();

        return services;
    }
}
