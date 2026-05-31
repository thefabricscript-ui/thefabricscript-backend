namespace TheFabricScript.Core.Entities;

public class CartItem : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; } // for guest cart
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public bool SavedForLater { get; set; } = false;

    // Navigation
    public User? User { get; set; }
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}
