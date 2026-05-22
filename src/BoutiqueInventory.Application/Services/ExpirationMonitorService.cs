using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BoutiqueInventory.Application.Services;

/// <summary>
/// Application-layer expiration scan: invoked by both the daily
/// background job and any manual trigger. Inspects every product
/// that expires within <c>windowDays</c> and creates an
/// <see cref="ExpirationAlert"/> when one does not already exist.
/// </summary>
public sealed class ExpirationMonitorService(
    IProductRepository products,
    IAlertRepository alerts,
    IUnitOfWork unitOfWork,
    IExpirationNotifier notifier,
    ILogger<ExpirationMonitorService> logger) : IExpirationMonitor
{
    /// <inheritdoc/>
    public async Task<int> RunAsync(int windowDays, CancellationToken ct)
    {
        if (windowDays <= 0) windowDays = 30;

        var now = DateTimeOffset.UtcNow;
        var dueProducts = await products.ListExpiringWithinAsync(windowDays, ct);
        if (dueProducts.Count == 0)
        {
            return 0;
        }

        var pendingNotifications = new List<ExpirationAlertNotification>();

        foreach (var product in dueProducts)
        {
            if (product.ExpirationDate is null) continue;

            if (await alerts.HasOpenAlertForProductAsync(product.Id, ct))
            {
                continue;
            }

            var daysUntil = (int)Math.Ceiling((product.ExpirationDate.Value - now).TotalDays);

            var alert = new ExpirationAlert
            {
                ProductId = product.Id,
                AlertDate = now,
                DaysUntilExpiration = daysUntil,
                IsAcknowledged = false
            };
            alerts.Add(alert);

            pendingNotifications.Add(new ExpirationAlertNotification(
                alert.Id,
                product.Id,
                product.Name,
                product.Sku,
                daysUntil,
                product.ExpirationDate,
                alert.AlertDate,
                product.WarehouseSection?.Warehouse?.Name,
                product.WarehouseSection?.Name));

            logger.LogWarning(
                "Product '{Name}' (SKU: {Sku}) expires in {Days} days",
                product.Name,
                product.Sku,
                daysUntil);
        }

        if (pendingNotifications.Count == 0)
        {
            return 0;
        }

        await unitOfWork.SaveChangesAsync(ct);

        foreach (var notification in pendingNotifications)
        {
            await notifier.NotifyNewAlertAsync(notification, ct);
        }

        return pendingNotifications.Count;
    }
}
