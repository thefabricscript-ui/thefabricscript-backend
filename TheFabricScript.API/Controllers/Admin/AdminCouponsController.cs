using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.DTOs.Admin;
using TheFabricScript.Core.Entities;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers.Admin;

[ApiController]
[Route("api/admin/coupons")]
[Authorize(Policy = "AdminOnly")]
public class AdminCouponsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public AdminCouponsController(IUnitOfWork uow) => _uow = uow;

    /// <summary>Get all coupons</summary>
    [HttpGet]
    public async Task<IActionResult> GetCoupons(
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _uow.Coupons.Query().AsQueryable();
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive);

        var total = await query.CountAsync();
        var coupons = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CouponListDto
            {
                Id = c.Id,
                Code = c.Code,
                Description = c.Description,
                DiscountType = c.DiscountType,
                DiscountValue = c.DiscountValue,
                MinOrderAmount = c.MinOrderAmount,
                MaxUses = c.MaxUses,
                UsedCount = c.UsedCount,
                IsActive = c.IsActive,
                IsFirstPurchaseOnly = c.IsFirstPurchaseOnly,
                ExpiresAt = c.ExpiresAt,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = coupons });
    }

    /// <summary>Create a new coupon</summary>
    [HttpPost]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponDto dto)
    {
        var code = dto.Code.Trim().ToUpper();
        if (await _uow.Coupons.ExistsAsync(c => c.Code == code))
            return Conflict(new { message = $"Coupon code '{code}' already exists" });

        var coupon = new Coupon
        {
            Code = code,
            Description = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MinOrderAmount = dto.MinOrderAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            MaxUses = dto.MaxUses,
            MaxUsesPerUser = dto.MaxUsesPerUser,
            IsFirstPurchaseOnly = dto.IsFirstPurchaseOnly,
            ExpiresAt = dto.ExpiresAt,
            IsActive = true
        };

        await _uow.Coupons.AddAsync(coupon);
        await _uow.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCoupon), new { id = coupon.Id }, coupon);
    }

    /// <summary>Get single coupon</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCoupon(Guid id)
    {
        var coupon = await _uow.Coupons.GetByIdAsync(id);
        if (coupon is null) return NotFound();
        return Ok(coupon);
    }

    /// <summary>Update coupon</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] CreateCouponDto dto)
    {
        var coupon = await _uow.Coupons.GetByIdAsync(id);
        if (coupon is null) return NotFound();

        var code = dto.Code.Trim().ToUpper();
        if (await _uow.Coupons.ExistsAsync(c => c.Code == code && c.Id != id))
            return Conflict(new { message = $"Code '{code}' taken by another coupon" });

        coupon.Code = code;
        coupon.Description = dto.Description;
        coupon.DiscountType = dto.DiscountType;
        coupon.DiscountValue = dto.DiscountValue;
        coupon.MinOrderAmount = dto.MinOrderAmount;
        coupon.MaxDiscountAmount = dto.MaxDiscountAmount;
        coupon.MaxUses = dto.MaxUses;
        coupon.MaxUsesPerUser = dto.MaxUsesPerUser;
        coupon.IsFirstPurchaseOnly = dto.IsFirstPurchaseOnly;
        coupon.ExpiresAt = dto.ExpiresAt;

        await _uow.Coupons.UpdateAsync(coupon);
        await _uow.SaveChangesAsync();
        return Ok(coupon);
    }

    /// <summary>Toggle coupon active/inactive</summary>
    [HttpPatch("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var coupon = await _uow.Coupons.GetByIdAsync(id);
        if (coupon is null) return NotFound();

        coupon.IsActive = !coupon.IsActive;
        await _uow.Coupons.UpdateAsync(coupon);
        await _uow.SaveChangesAsync();
        return Ok(new { id, isActive = coupon.IsActive });
    }

    /// <summary>Validate a coupon code (used by checkout)</summary>
    [HttpPost("validate")]
    [Authorize] // any logged-in user
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest req)
    {
        var code = req.Code.Trim().ToUpper();
        var coupon = await _uow.Coupons.FirstOrDefaultAsync(c => c.Code == code && c.IsActive);

        if (coupon is null) return BadRequest(new { message = "Invalid or expired coupon code" });
        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "Coupon has expired" });
        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses)
            return BadRequest(new { message = "Coupon usage limit reached" });
        if (coupon.MinOrderAmount.HasValue && req.OrderTotal < coupon.MinOrderAmount)
            return BadRequest(new { message = $"Minimum order amount of ₹{coupon.MinOrderAmount} required" });

        var discount = coupon.DiscountType == "Percentage"
            ? req.OrderTotal * (coupon.DiscountValue / 100)
            : coupon.DiscountValue;

        if (coupon.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);

        return Ok(new
        {
            valid = true,
            coupon.Code,
            coupon.Description,
            coupon.DiscountType,
            coupon.DiscountValue,
            DiscountAmount = Math.Round(discount, 2),
            FinalTotal = Math.Round(req.OrderTotal - discount, 2)
        });
    }
}

public record ValidateCouponRequest(string Code, decimal OrderTotal);
