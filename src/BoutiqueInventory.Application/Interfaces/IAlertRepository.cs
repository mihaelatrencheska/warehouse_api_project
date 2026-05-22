using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Interfaces;

/// <summary>Persistence operations for <see cref="ExpirationAlert"/>.</summary>
public interface IAlertRepository
{
    Task<IReadOnlyList<ExpirationAlert>> ListUnacknowledgedAsync(CancellationToken ct);

    Task<ExpirationAlert?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// True when there is at least one un-acknowledged
    /// <see cref="ExpirationAlert"/> targeting the given product.
    /// Used by the monitor to avoid raising duplicate alerts; once the
    /// owner acknowledges the existing alert, a fresh one can fire.
    /// </summary>
    Task<bool> HasOpenAlertForProductAsync(Guid productId, CancellationToken ct);

    void Add(ExpirationAlert alert);
    void Update(ExpirationAlert alert);
}
