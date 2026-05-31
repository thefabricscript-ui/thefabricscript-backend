namespace TheFabricScript.Core.Entities;

public class Shipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public string? AwbNumber { get; set; }
    public string? CourierName { get; set; }
    public string? TrackingUrl { get; set; }
    public string? ShiprocketOrderId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
