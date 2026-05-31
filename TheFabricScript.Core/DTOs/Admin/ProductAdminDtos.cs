namespace TheFabricScript.Core.DTOs.Admin;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public string? SKU { get; set; }
    public string? Brand { get; set; }
    public string? Material { get; set; }
    public string? CareInstructions { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsFeatured { get; set; } = false;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public List<CreateVariantDto> Variants { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
}

public class UpdateProductDto : CreateProductDto
{
    public bool IsActive { get; set; } = true;
}

public class CreateVariantDto
{
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? ColorHex { get; set; }
    public string? Material { get; set; }
    public int Stock { get; set; }
    public decimal? PriceOverride { get; set; }
    public string? SKU { get; set; }
}

public class ProductAdminListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public string? SKU { get; set; }
    public string? Brand { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BulkUploadResultDto
{
    public int TotalRows { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
