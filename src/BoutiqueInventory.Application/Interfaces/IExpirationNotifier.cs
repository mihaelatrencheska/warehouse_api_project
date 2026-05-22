namespace BoutiqueInventory.Application.Interfaces;

/// <summary>Proactive delivery channel when a new expiration alert is created.</summary>
public interface IExpirationNotifier
{
    Task NotifyNewAlertAsync(ExpirationAlertNotification notification, CancellationToken ct);
}

/// <summary>Payload pushed to email, webhooks, and real-time clients.</summary>
public sealed record ExpirationAlertNotification(
    Guid AlertId,
    Guid ProductId,
    string ProductName,
    string Sku,
    int DaysUntilExpiration,
    DateTimeOffset? ExpirationDate,
    DateTimeOffset AlertDate,
    string? WarehouseName,
    string? SectionName);
