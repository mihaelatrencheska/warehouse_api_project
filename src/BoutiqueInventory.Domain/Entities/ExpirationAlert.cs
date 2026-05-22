namespace BoutiqueInventory.Domain.Entities;

/// <summary>
/// Persistent notification produced by the expiration monitor when a
/// product is within (or past) its 30-day expiration window.
/// </summary>
public class ExpirationAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>The moment this alert was created.</summary>
    public DateTimeOffset AlertDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Days between <see cref="AlertDate"/> and the product's
    /// expiration date. May be negative when the item has already
    /// expired by the time the alert is raised.
    /// </summary>
    public int DaysUntilExpiration { get; set; }

    public bool IsAcknowledged { get; set; }

    public DateTimeOffset? AcknowledgedAt { get; set; }
}
