using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.Entities;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = "AdminOnly")]
public class AdminCategoriesController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public AdminCategoriesController(IUnitOfWork uow) => _uow = uow;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cats = await _uow.Categories.Query()
            .IgnoreQueryFilters()
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
        return Ok(cats);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequest req)
    {
        if (await _uow.Categories.ExistsAsync(c => c.Slug == req.Slug))
            return Conflict(new { message = "Slug already exists" });

        var cat = new Category
        {
            Name = req.Name,
            Slug = req.Slug.ToLower(),
            Description = req.Description,
            ImageUrl = req.ImageUrl,
            ParentId = req.ParentId,
            SortOrder = req.SortOrder,
            IsActive = true
        };
        await _uow.Categories.AddAsync(cat);
        await _uow.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = cat.Id }, cat);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoryRequest req)
    {
        var cat = await _uow.Categories.GetByIdAsync(id);
        if (cat is null) return NotFound();

        cat.Name = req.Name;
        cat.Slug = req.Slug.ToLower();
        cat.Description = req.Description;
        cat.ImageUrl = req.ImageUrl;
        cat.ParentId = req.ParentId;
        cat.SortOrder = req.SortOrder;

        await _uow.Categories.UpdateAsync(cat);
        await _uow.SaveChangesAsync();
        return Ok(cat);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _uow.Categories.DeleteAsync(id);
        await _uow.SaveChangesAsync();
        return NoContent();
    }
}

public record CategoryRequest(
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentId,
    int SortOrder = 0);
