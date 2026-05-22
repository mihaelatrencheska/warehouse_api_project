using System.Net;
using System.Net.Mail;
using BoutiqueInventory.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BoutiqueInventory.Infrastructure.Notifications;

public sealed class EmailNotificationOptions
{
    public bool Enabled { get; set; }
    public string? ToAddress { get; set; }
    public string? FromAddress { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public sealed class EmailExpirationNotifier(
    IOptions<EmailNotificationOptions> options,
    ILogger<EmailExpirationNotifier> logger) : IExpirationNotifier
{
    public async Task NotifyNewAlertAsync(ExpirationAlertNotification notification, CancellationToken ct)
    {
        var cfg = options.Value;
        if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.ToAddress) || string.IsNullOrWhiteSpace(cfg.SmtpHost))
        {
            return;
        }

        var subject = $"[Boutique] {notification.ProductName} expires in {notification.DaysUntilExpiration} days";
        var body = $"""
            Product: {notification.ProductName} (SKU: {notification.Sku})
            Expires: {notification.ExpirationDate:yyyy-MM-dd}
            Days remaining: {notification.DaysUntilExpiration}
            Location: {notification.WarehouseName} / {notification.SectionName}
            Alert ID: {notification.AlertId}
            """;

        using var message = new MailMessage(
            cfg.FromAddress ?? cfg.Username ?? "boutique@localhost",
            cfg.ToAddress,
            subject,
            body);

        using var client = new SmtpClient(cfg.SmtpHost, cfg.SmtpPort)
        {
            EnableSsl = cfg.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(cfg.Username))
        {
            client.Credentials = new NetworkCredential(cfg.Username, cfg.Password);
        }

        try
        {
            await client.SendMailAsync(message, ct);
            logger.LogInformation("Expiration alert email sent for product {Sku}", notification.Sku);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send expiration alert email for product {Sku}", notification.Sku);
        }
    }
}
