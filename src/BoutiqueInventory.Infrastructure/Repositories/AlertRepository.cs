using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using BoutiqueInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BoutiqueInventory.Infrastructure.Repositories;

/// <inheritdoc cref="IAlertRepository"/>
public sealed class AlertRepository(AppDbContext db) : IAlertRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExpirationAlert>> ListUnacknowledgedAsync(CancellationToken ct) =>
        await db.ExpirationAlerts.AsNoTracking()
            .Include(a => a.Product)
            .Where(a => !a.IsAcknowledged)
            .OrderBy(a => a.DaysUntilExpiration)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public Task<ExpirationAlert?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.ExpirationAlerts.FirstOrDefaultAsync(a => a.Id == id, ct);

    /// <inheritdoc/>
    public Task<bool> HasOpenAlertForProductAsync(Guid productId, CancellationToken ct) =>
        db.ExpirationAlerts.AsNoTracking()
            .AnyAsync(a => a.ProductId == productId && !a.IsAcknowledged, ct);

    /// <inheritdoc/>
    public void Add(ExpirationAlert alert) => db.ExpirationAlerts.Add(alert);

    /// <inheritdoc/>
    public void Update(ExpirationAlert alert) => db.ExpirationAlerts.Update(alert);
}
