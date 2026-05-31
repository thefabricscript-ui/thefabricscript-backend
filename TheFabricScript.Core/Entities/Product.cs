namespace TheFabricScript.Core.Entities;

/// <summary>
/// Represents a product listed in the catalogue.
/// A product is the parent record; size/colour variants are stored in
/// <see cref="ProductVariant"/> and images in <see cref="ProductImage"/>.
/// </summary>
public class Product : BaseEntity
{
    /// <summary>Display name of the product. Shown in listings and on the product page.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe unique identifier used in product page URLs
    /// (e.g. <c>/products/floral-kurta-blue</c>).
    /// Must be lowercase, hyphen-separated, and unique across all products.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Full HTML description shown on the product detail page.</summary>
    public string? Description { get; set; }

    /// <summary>Brief plain-text description used in listing cards and meta tags.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// MRP / base selling price in INR.
    /// If <see cref="DiscountPrice"/> is set, this is shown as the strikethrough price.
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Discounted selling price in INR. When set, this is the actual checkout price.
    /// The discount percentage is calculated as <c>(BasePrice - DiscountPrice) / BasePrice * 100</c>.
    /// </summary>
    public decimal? DiscountPrice { get; set; }

    /// <summary>
    /// Total available stock across all variants.
    /// Updated by the admin or automatically decremented on order confirmation.
    /// Low-stock alert triggers when this falls to 5 or below.
    /// </summary>
    public int Stock { get; set; } = 0;

    /// <summary>Stock Keeping Unit — internal unique product code for inventory management.</summary>
    public string? SKU { get; set; }

    /// <summary>Brand name (e.g. "The Fabric Script Originals", "Libas").</summary>
    public string? Brand { get; set; }

    /// <summary>Primary fabric/material (e.g. "Cotton", "Georgette", "Silk Blend").</summary>
    public string? Material { get; set; }

    /// <summary>Washing and care instructions displayed on the product page.</summary>
    public string? CareInstructions { get; set; }

    /// <summary>
    /// Controls visibility to customers. Inactive products are excluded from all
    /// public-facing queries but remain in admin views.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When true, the product appears in "Featured Products" sections on the homepage.</summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Cumulative view count. Incremented on every call to
    /// <c>GET /api/products/{slug}</c>. Used for popularity sorting and analytics.
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>Foreign key to <see cref="Category"/>. Required.</summary>
    public Guid CategoryId { get; set; }

    // ── SEO ───────────────────────────────────────────────

    /// <summary>
    /// Custom HTML &lt;title&gt; tag for the product page.
    /// Falls back to <see cref="Name"/> if not set.
    /// </summary>
    public string? MetaTitle { get; set; }

    /// <summary>
    /// HTML meta description for search engines.
    /// Recommended length: 150–160 characters.
    /// </summary>
    public string? MetaDescription { get; set; }

    // ── Navigation Properties ─────────────────────────────

    /// <summary>Parent category. Configured as Restrict delete — category cannot be deleted while products exist.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Size/colour/material variants. Cascade deleted with the product.</summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    /// <summary>Product images and videos. Cascade deleted with the product.</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    /// <summary>Approved and pending customer reviews.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Users who have wishlisted this product.</summary>
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

    /// <summary>Line items in orders that include this product.</summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>Active cart items referencing this product.</summary>
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
