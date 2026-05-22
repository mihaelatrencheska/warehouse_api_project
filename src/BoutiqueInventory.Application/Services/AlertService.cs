using AutoMapper;
using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Services;

/// <inheritdoc cref="IAlertService"/>
public sealed class AlertService(
    IAlertRepository alerts,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IAlertService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExpirationAlertResponse>> ListUnacknowledgedAsync(CancellationToken ct)
    {
        var entities = await alerts.ListUnacknowledgedAsync(ct);
        return mapper.Map<IReadOnlyList<ExpirationAlertResponse>>(entities);
    }

    /// <inheritdoc/>
    public async Task AcknowledgeAsync(Guid alertId, CancellationToken ct)
    {
        var alert = await alerts.GetByIdAsync(alertId, ct)
            ?? throw new NotFoundException(nameof(ExpirationAlert), alertId);

        if (alert.IsAcknowledged) return;

        alert.IsAcknowledged = true;
        alert.AcknowledgedAt = DateTimeOffset.UtcNow;
        alerts.Update(alert);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
