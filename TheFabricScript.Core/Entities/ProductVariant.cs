namespace TheFabricScript.Core.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? ColorHex { get; set; }
    public string? Material { get; set; }
    public int Stock { get; set; } = 0;
    public decimal? PriceOverride { get; set; }
    public string? SKU { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Product Product { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
