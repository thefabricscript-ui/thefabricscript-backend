namespace TheFabricScript.Core.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public string? MediaType { get; set; } = "image"; // image | video

    // Navigation
    public Product Product { get; set; } = null!;
}
