namespace TheFabricScript.Core.Entities;

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public Guid? ChangedByUserId { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
