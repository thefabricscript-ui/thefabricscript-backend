using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers;

/// <summary>
/// Public product catalogue endpoints.
/// No authentication required — all routes are accessible to guests and logged-in users.
/// </summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the controller with the Unit of Work.</summary>
    public ProductsController(IUnitOfWork uow) => _uow = uow;

    /// <summary>
    /// Returns a paginated, filterable list of active products.
    /// </summary>
    /// <remarks>
    /// Supports combining multiple filters in one request:
    /// <code>
    /// GET /api/products?category=sarees&amp;minPrice=500&amp;maxPrice=2000&amp;color=Red&amp;sort=popular&amp;page=1&amp;pageSize=24
    /// </code>
    ///
    /// **Sort options:**
    /// - `newest` (default) — most recently added first
    /// - `price_asc` — cheapest first
    /// - `price_desc` — most expensive first
    /// - `popular` — highest view count first
    ///
    /// **Response shape:**
    /// ```json
    /// { "total": 120, "page": 1, "pageSize": 24, "data": [ ... ] }
    /// ```
    /// </remarks>
    /// <param name="category">Category slug to filter by. Matches both the category itself and its children.</param>
    /// <param name="minPrice">Minimum base price in INR (inclusive).</param>
    /// <param name="maxPrice">Maximum base price in INR (inclusive).</param>
    /// <param name="color">Filter by variant colour name (case-sensitive).</param>
    /// <param name="size">Filter by variant size (e.g. "S", "M", "L", "XL", "Free Size").</param>
    /// <param name="sort">Sort order: newest | price_asc | price_desc | popular. Default: newest.</param>
    /// <param name="page">1-based page number. Default: 1.</param>
    /// <param name="pageSize">Items per page. Max recommended: 48. Default: 24.</param>
    /// <response code="200">Paginated product list.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? color,
        [FromQuery] string? size,
        [FromQuery] string? sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24)
    {
        var query = _uow.Products.Query()
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category.Slug == category || p.Category.Parent!.Slug == category);

        if (minPrice.HasValue) query = query.Where(p => p.BasePrice >= minPrice);
        if (maxPrice.HasValue) query = query.Where(p => p.BasePrice <= maxPrice);

        if (!string.IsNullOrEmpty(color))
            query = query.Where(p => p.Variants.Any(v => v.Color == color && v.IsActive));

        if (!string.IsNullOrEmpty(size))
            query = query.Where(p => p.Variants.Any(v => v.Size == size && v.IsActive));

        query = sort switch
        {
            "price_asc"  => query.OrderBy(p => p.DiscountPrice ?? p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.BasePrice),
            "popular"    => query.OrderByDescending(p => p.ViewCount),
            _            => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { total, page, pageSize, data = products });
    }

    /// <summary>
    /// Returns full product details including variants, images, category, and approved reviews.
    /// Also increments the product's view counter (used for popularity ranking).
    /// </summary>
    /// <param name="slug">URL-safe product identifier (e.g. <c>floral-cotton-kurta-blue</c>).</param>
    /// <response code="200">Full product detail object.</response>
    /// <response code="404">No active product found with the given slug.</response>
    [HttpGet("{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(string slug)
    {
        var product = await _uow.Products.Query()
            .Include(p => p.Images)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Category)
            .Include(p => p.Reviews.Where(r => r.IsApproved))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product is null) return NotFound();

        product.ViewCount++;
        await _uow.SaveChangesAsync();

        return Ok(product);
    }

    /// <summary>
    /// Full-text search across product name, description, brand, and category name.
    /// Results are ordered by popularity (view count) descending.
    /// </summary>
    /// <remarks>
    /// Current implementation uses SQL LIKE. Migrate to Elasticsearch or Typesense
    /// when product catalogue exceeds ~5,000 items for better performance and relevance.
    /// </remarks>
    /// <param name="q">Search query string. Minimum 1 character.</param>
    /// <param name="page">1-based page number. Default: 1.</param>
    /// <param name="pageSize">Items per page. Default: 24.</param>
    /// <response code="200">Search results with the original query echoed back.</response>
    /// <response code="400">Query string is empty or whitespace.</response>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query is required" });

        var lower = q.ToLower();
        var results = await _uow.Products.Query()
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive &&
                (p.Name.ToLower().Contains(lower) ||
                 p.Description!.ToLower().Contains(lower) ||
                 p.Brand!.ToLower().Contains(lower) ||
                 p.Category.Name.ToLower().Contains(lower)))
            .OrderByDescending(p => p.ViewCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { query = q, total = results.Count, data = results });
    }
}
