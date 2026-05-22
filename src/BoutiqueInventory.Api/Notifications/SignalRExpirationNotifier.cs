using BoutiqueInventory.Api.Hubs;
using BoutiqueInventory.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BoutiqueInventory.Api.Notifications;

public sealed class SignalRExpirationNotifier(IHubContext<AlertsHub> hub) : IExpirationNotifier
{
    public Task NotifyNewAlertAsync(ExpirationAlertNotification notification, CancellationToken ct) =>
        hub.Clients.All.SendAsync(AlertsHub.NewAlertEvent, notification, ct);
}
