namespace TheFabricScript.Core.DTOs.Admin;

public class OrderAdminListDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

public class UpdateShipmentDto
{
    public string? AwbNumber { get; set; }
    public string? CourierName { get; set; }
    public string? TrackingUrl { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
}
