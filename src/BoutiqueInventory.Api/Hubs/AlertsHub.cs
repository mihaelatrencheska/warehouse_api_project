using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoutiqueInventory.Api.Hubs;

[Authorize]
public sealed class AlertsHub : Hub
{
    public const string NewAlertEvent = "ReceiveAlert";
}
