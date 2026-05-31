using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.DTOs.Admin;
using TheFabricScript.Core.Entities;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = "AdminOnly")]
public class AdminProductsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public AdminProductsController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Get all products (admin view, includes inactive)</summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] string? stockAlert,  // "low" | "out"
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _uow.Products.Query()
            .IgnoreQueryFilters()   // include soft-deleted for admin
            .Include(p => p.Category)
            .Include(p => p.Images)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || p.SKU!.Contains(search) || p.Brand!.Contains(search));

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive);

        query = stockAlert switch
        {
            "out" => query.Where(p => p.Stock == 0),
            "low" => query.Where(p => p.Stock > 0 && p.Stock <= 5),
            _ => query
        };

        var total = await query.CountAsync();

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductAdminListDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                BasePrice = p.BasePrice,
                DiscountPrice = p.DiscountPrice,
                Stock = p.Stock,
                SKU = p.SKU,
                Brand = p.Brand,
                CategoryName = p.Category.Name,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                ViewCount = p.ViewCount,
                PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)!.Url,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = products });
    }

    /// <summary>Get single product by ID (admin)</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _uow.Products.Query()
            .IgnoreQueryFilters()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();
        return Ok(product);
    }

    /// <summary>Create a new product</summary>
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        // Check slug uniqueness
        if (await _uow.Products.ExistsAsync(p => p.Slug == dto.Slug))
            return Conflict(new { message = $"Slug '{dto.Slug}' already exists" });

        var product = new Product
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            ShortDescription = dto.ShortDescription,
            BasePrice = dto.BasePrice,
            DiscountPrice = dto.DiscountPrice,
            Stock = dto.Stock,
            SKU = dto.SKU,
            Brand = dto.Brand,
            Material = dto.Material,
            CareInstructions = dto.CareInstructions,
            CategoryId = dto.CategoryId,
            IsFeatured = dto.IsFeatured,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            IsActive = true
        };

        // Add variants
        foreach (var v in dto.Variants)
        {
            product.Variants.Add(new ProductVariant
            {
                Size = v.Size,
                Color = v.Color,
                ColorHex = v.ColorHex,
                Material = v.Material,
                Stock = v.Stock,
                PriceOverride = v.PriceOverride,
                SKU = v.SKU
            });
        }

        // Add images
        for (int i = 0; i < dto.ImageUrls.Count; i++)
        {
            product.Images.Add(new ProductImage
            {
                Url = dto.ImageUrls[i],
                IsPrimary = i == 0,
                SortOrder = i
            });
        }

        await _uow.Products.AddAsync(product);
        await _uow.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>Update product</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
    {
        var product = await _uow.Products.Query()
            .IgnoreQueryFilters()
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        // Check slug uniqueness (exclude self)
        if (await _uow.Products.ExistsAsync(p => p.Slug == dto.Slug && p.Id != id))
            return Conflict(new { message = $"Slug '{dto.Slug}' already taken" });

        product.Name = dto.Name;
        product.Slug = dto.Slug;
        product.Description = dto.Description;
        product.ShortDescription = dto.ShortDescription;
        product.BasePrice = dto.BasePrice;
        product.DiscountPrice = dto.DiscountPrice;
        product.Stock = dto.Stock;
        product.SKU = dto.SKU;
        product.Brand = dto.Brand;
        product.Material = dto.Material;
        product.CareInstructions = dto.CareInstructions;
        product.CategoryId = dto.CategoryId;
        product.IsFeatured = dto.IsFeatured;
        product.IsActive = dto.IsActive;
        product.MetaTitle = dto.MetaTitle;
        product.MetaDescription = dto.MetaDescription;

        await _uow.Products.UpdateAsync(product);
        await _uow.SaveChangesAsync();

        return Ok(product);
    }

    /// <summary>Toggle product active/inactive (soft toggle)</summary>
    [HttpPatch("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var product = await _uow.Products.GetByIdAsync(id);
        if (product is null) return NotFound();

        product.IsActive = !product.IsActive;
        await _uow.Products.UpdateAsync(product);
        await _uow.SaveChangesAsync();

        return Ok(new { id, isActive = product.IsActive });
    }

    /// <summary>Soft-delete product</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        if (!await _uow.Products.ExistsAsync(p => p.Id == id))
            return NotFound();

        await _uow.Products.DeleteAsync(id);
        await _uow.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Update stock for a variant or product</summary>
    [HttpPatch("{id:guid}/stock")]
    public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockRequest req)
    {
        if (req.VariantId.HasValue)
        {
            var variant = await _uow.ProductVariants.GetByIdAsync(req.VariantId.Value);
            if (variant is null || variant.ProductId != id) return NotFound();
            variant.Stock = req.Stock;
            await _uow.ProductVariants.UpdateAsync(variant);
        }
        else
        {
            var product = await _uow.Products.GetByIdAsync(id);
            if (product is null) return NotFound();
            product.Stock = req.Stock;
            await _uow.Products.UpdateAsync(product);
        }

        await _uow.SaveChangesAsync();
        return Ok(new { message = "Stock updated" });
    }

    /// <summary>Bulk upload products via CSV</summary>
    [HttpPost("bulk-upload")]
    public async Task<IActionResult> BulkUpload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "CSV file is required" });

        var result = new BulkUploadResultDto();
        using var reader = new System.IO.StreamReader(file.OpenReadStream());

        string? line;
        int row = 0;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (row++ == 0) continue; // skip header

            try
            {
                var cols = line.Split(',');
                if (cols.Length < 6) throw new ArgumentException("Insufficient columns");

                var product = new Product
                {
                    Name = cols[0].Trim(),
                    Slug = cols[1].Trim().ToLower().Replace(" ", "-"),
                    BasePrice = decimal.Parse(cols[2].Trim()),
                    Stock = int.Parse(cols[3].Trim()),
                    SKU = cols[4].Trim(),
                    Brand = cols[5].Trim(),
                    IsActive = true
                };

                await _uow.Products.AddAsync(product);
                result.Succeeded++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Row {row}: {ex.Message}");
            }
        }

        await _uow.SaveChangesAsync();
        result.TotalRows = row - 1;
        return Ok(result);
    }
}

public record UpdateStockRequest(int Stock, Guid? VariantId = null);
