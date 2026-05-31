using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public CategoriesController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Get full category tree</summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _uow.Categories.Query()
            .Where(c => c.IsActive && c.ParentId == null)
            .Include(c => c.Children.Where(ch => ch.IsActive))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Get single category by slug</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCategory(string slug)
    {
        var category = await _uow.Categories.Query()
            .Include(c => c.Children.Where(ch => ch.IsActive))
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);

        if (category is null) return NotFound();
        return Ok(category);
    }
}
