using BoutiqueInventory.Application.Interfaces;

namespace BoutiqueInventory.Infrastructure.Notifications;

/// <summary>Invokes every registered notification channel.</summary>
public sealed class CompositeExpirationNotifier(IEnumerable<IExpirationNotifier> notifiers) : IExpirationNotifier
{
    public async Task NotifyNewAlertAsync(ExpirationAlertNotification notification, CancellationToken ct)
    {
        foreach (var notifier in notifiers)
        {
            await notifier.NotifyNewAlertAsync(notification, ct);
        }
    }
}
