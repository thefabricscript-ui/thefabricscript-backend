using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.Entities;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public CartController(IUnitOfWork uow) => _uow = uow;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var items = await _uow.CartItems.Query()
            .Include(c => c.Product).ThenInclude(p => p.Images)
            .Include(c => c.Variant)
            .Where(c => c.UserId == CurrentUserId && !c.SavedForLater)
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        var existing = await _uow.CartItems.FirstOrDefaultAsync(c =>
            c.UserId == CurrentUserId &&
            c.ProductId == request.ProductId &&
            c.VariantId == request.VariantId &&
            !c.SavedForLater);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
            await _uow.CartItems.UpdateAsync(existing);
        }
        else
        {
            await _uow.CartItems.AddAsync(new CartItem
            {
                UserId = CurrentUserId,
                ProductId = request.ProductId,
                VariantId = request.VariantId,
                Quantity = request.Quantity
            });
        }
        await _uow.SaveChangesAsync();
        return Ok(new { message = "Added to cart" });
    }

    [HttpPut("{itemId}")]
    public async Task<IActionResult> UpdateQuantity(Guid itemId, [FromBody] UpdateCartRequest request)
    {
        var item = await _uow.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == CurrentUserId);
        if (item is null) return NotFound();

        if (request.Quantity <= 0)
        {
            await _uow.CartItems.DeleteAsync(itemId);
        }
        else
        {
            item.Quantity = request.Quantity;
            await _uow.CartItems.UpdateAsync(item);
        }
        await _uow.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> RemoveFromCart(Guid itemId)
    {
        var item = await _uow.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == CurrentUserId);
        if (item is null) return NotFound();
        await _uow.CartItems.DeleteAsync(itemId);
        await _uow.SaveChangesAsync();
        return Ok();
    }
}

public record AddToCartRequest(Guid ProductId, Guid? VariantId, int Quantity = 1);
public record UpdateCartRequest(int Quantity);
