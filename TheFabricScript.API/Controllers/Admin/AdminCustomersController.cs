using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers.Admin;

[ApiController]
[Route("api/admin/customers")]
[Authorize(Policy = "AdminOnly")]
public class AdminCustomersController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public AdminCustomersController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Get all customers with activity summary</summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _uow.Users.Query()
            .Where(u => u.Role == "Customer");

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email.Contains(search) ||
                u.Phone!.Contains(search));

        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive);

        var total = await query.CountAsync();

        var customers = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Phone,
                u.IsActive,
                u.IsEmailVerified,
                u.CreatedAt,
                TotalOrders = u.Orders.Count,
                TotalSpent = u.Orders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.Total)
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = customers });
    }

    /// <summary>Get single customer profile + order history</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var user = await _uow.Users.Query()
            .Include(u => u.Orders)
                .ThenInclude(o => o.Items)
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == id && u.Role == "Customer");

        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Phone,
            user.ProfileImageUrl,
            user.IsActive,
            user.IsEmailVerified,
            user.IsPhoneVerified,
            user.CreatedAt,
            Addresses = user.Addresses,
            Orders = user.Orders.OrderByDescending(o => o.CreatedAt).Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.PaymentStatus,
                o.Total,
                o.CreatedAt,
                ItemCount = o.Items.Count
            }),
            Stats = new
            {
                TotalOrders = user.Orders.Count,
                TotalSpent = user.Orders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.Total),
                LastOrderAt = user.Orders.MaxBy(o => o.CreatedAt)?.CreatedAt
            }
        });
    }

    /// <summary>Deactivate / reactivate a customer account</summary>
    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user is null || user.Role != "Customer") return NotFound();

        user.IsActive = !user.IsActive;
        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();

        return Ok(new { id, isActive = user.IsActive });
    }
}
