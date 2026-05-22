using System.Net.Http.Json;
using BoutiqueInventory.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoutiqueInventory.Infrastructure.Notifications;

public sealed class WebhookNotificationOptions
{
    public bool Enabled { get; set; }
    public string? Url { get; set; }
}

public sealed class WebhookExpirationNotifier(
    IHttpClientFactory httpClientFactory,
    IOptions<WebhookNotificationOptions> options,
    ILogger<WebhookExpirationNotifier> logger) : IExpirationNotifier
{
    public async Task NotifyNewAlertAsync(ExpirationAlertNotification notification, CancellationToken ct)
    {
        var cfg = options.Value;
        if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.Url))
        {
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(WebhookExpirationNotifier));
            var response = await client.PostAsJsonAsync(cfg.Url, notification, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Webhook notification returned {StatusCode} for alert {AlertId}",
                    (int)response.StatusCode,
                    notification.AlertId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Webhook notification failed for alert {AlertId}", notification.AlertId);
        }
    }
}
