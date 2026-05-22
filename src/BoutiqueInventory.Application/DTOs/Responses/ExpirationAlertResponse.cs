namespace BoutiqueInventory.Application.DTOs.Responses;

/// <summary>Alert raised by the expiration monitor.</summary>
public sealed class ExpirationAlertResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public DateTimeOffset? ExpirationDate { get; set; }
    public DateTimeOffset AlertDate { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
}
