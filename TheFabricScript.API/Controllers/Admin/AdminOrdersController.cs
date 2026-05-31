using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.DTOs.Admin;
using TheFabricScript.Core.Entities;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers.Admin;

/// <summary>
/// Admin order management endpoints.
/// Provides full visibility into all customer orders, status management,
/// shipment tracking, and refund processing.
/// Requires <c>Admin</c> or <c>SuperAdmin</c> role.
/// </summary>
[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
[Tags("Admin — Orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the controller with the Unit of Work.</summary>
    public AdminOrdersController(IUnitOfWork uow) => _uow = uow;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>
    /// Returns a paginated, filterable list of all orders across all customers.
    /// </summary>
    /// <remarks>
    /// **Valid status values:** Pending | Confirmed | Packed | Shipped | Delivered | Cancelled | ReturnRequested | Returned
    ///
    /// **Valid paymentStatus values:** Pending | Paid | Failed | Refunded
    ///
    /// **Search** matches against: order number, customer email, customer phone.
    ///
    /// **Date range** filters use UTC timestamps. Example: `from=2026-01-01&amp;to=2026-01-31`.
    /// </remarks>
    /// <param name="status">Filter by order status.</param>
    /// <param name="paymentStatus">Filter by payment status.</param>
    /// <param name="search">Search by order number, customer email, or phone.</param>
    /// <param name="from">Start of date range (inclusive, UTC).</param>
    /// <param name="to">End of date range (inclusive, UTC).</param>
    /// <param name="page">1-based page number. Default: 1.</param>
    /// <param name="pageSize">Items per page. Default: 25.</param>
    /// <response code="200">Paginated order list.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _uow.Orders.Query()
            .Include(o => o.User)
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status)) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrEmpty(paymentStatus)) query = query.Where(o => o.PaymentStatus == paymentStatus);
        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                o.User.Email.Contains(search) ||
                o.User.Phone!.Contains(search));

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderAdminListDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.User.FirstName + " " + o.User.LastName,
                CustomerEmail = o.User.Email,
                Total = o.Total,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = orders });
    }

    /// <summary>
    /// Returns complete order detail including line items, shipping address,
    /// coupon used, shipment tracking info, and full status history timeline.
    /// </summary>
    /// <param name="id">Order GUID.</param>
    /// <response code="200">Full order detail.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.User)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Items).ThenInclude(i => i.Variant)
            .Include(o => o.Coupon)
            .Include(o => o.Shipment)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();
        return Ok(order);
    }

    /// <summary>
    /// Updates the status of an order and appends a record to the status history timeline.
    /// </summary>
    /// <remarks>
    /// **Allowed status transitions (enforced by business logic, not this endpoint):**
    /// ```
    /// Pending → Confirmed → Packed → Shipped → Delivered
    ///                                        ↘ ReturnRequested → Returned
    /// Any → Cancelled (if not yet Shipped)
    /// ```
    ///
    /// Setting status to `Delivered` automatically sets the <c>DeliveredAt</c> timestamp.
    /// Setting status to `Cancelled` sets <c>CancelledAt</c>.
    ///
    /// A <see cref="OrderStatusHistory"/> record is always created, recording the actor
    /// (the admin's user ID) and optional comment.
    /// </remarks>
    /// <param name="id">Order GUID.</param>
    /// <param name="dto">New status and optional comment.</param>
    /// <response code="200">Status updated. Returns old and new status values.</response>
    /// <response code="400">Invalid status value provided.</response>
    /// <response code="404">Order not found.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var validStatuses = new[] { "Confirmed", "Packed", "Shipped", "Delivered", "Cancelled", "ReturnRequested", "Returned" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });

        var order = await _uow.Orders.GetByIdAsync(id);
        if (order is null) return NotFound();

        var oldStatus = order.Status;
        order.Status = dto.Status;

        if (dto.Status == "Delivered") order.DeliveredAt = DateTime.UtcNow;
        if (dto.Status == "Cancelled") order.CancelledAt = DateTime.UtcNow;

        await _uow.OrderStatusHistories.AddAsync(new OrderStatusHistory
        {
            OrderId = id,
            Status = dto.Status,
            Comment = dto.Comment ?? $"Status changed from {oldStatus} to {dto.Status}",
            ChangedByUserId = CurrentUserId
        });

        await _uow.Orders.UpdateAsync(order);
        await _uow.SaveChangesAsync();

        return Ok(new { orderId = id, oldStatus, newStatus = dto.Status });
    }

    /// <summary>
    /// Attaches or updates shipment details (AWB number, courier, tracking URL).
    /// Automatically sets order status to <c>Shipped</c> when a new shipment is created.
    /// </summary>
    /// <remarks>
    /// If no <see cref="Shipment"/> record exists for the order, one is created.
    /// If one already exists, it is updated in place.
    ///
    /// After calling this endpoint, the tracking URL becomes visible to the customer
    /// on their order detail page.
    /// </remarks>
    /// <param name="id">Order GUID.</param>
    /// <param name="dto">AWB number, courier name, tracking URL, and estimated delivery date.</param>
    /// <response code="200">Shipment created or updated.</response>
    /// <response code="404">Order not found.</response>
    [HttpPatch("{id:guid}/shipment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShipment(Guid id, [FromBody] UpdateShipmentDto dto)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        if (order.Shipment is null)
        {
            await _uow.Shipments.AddAsync(new Shipment
            {
                OrderId = id,
                AwbNumber = dto.AwbNumber,
                CourierName = dto.CourierName,
                TrackingUrl = dto.TrackingUrl,
                EstimatedDelivery = dto.EstimatedDelivery,
                ShippedAt = DateTime.UtcNow,
                Status = "Shipped"
            });
            order.Status = "Shipped";
        }
        else
        {
            order.Shipment.AwbNumber = dto.AwbNumber;
            order.Shipment.CourierName = dto.CourierName;
            order.Shipment.TrackingUrl = dto.TrackingUrl;
            order.Shipment.EstimatedDelivery = dto.EstimatedDelivery;
            await _uow.Shipments.UpdateAsync(order.Shipment);
        }

        await _uow.Orders.UpdateAsync(order);
        await _uow.SaveChangesAsync();

        return Ok(new { message = "Shipment updated" });
    }

    /// <summary>
    /// Marks an order as refunded.
    /// </summary>
    /// <remarks>
    /// **Current behaviour:** Updates <c>PaymentStatus</c> to <c>"Refunded"</c> and
    /// <c>Status</c> to <c>"Returned"</c> in the database, then appends a history entry.
    ///
    /// **TODO:** Wire up the Razorpay Refunds API
    /// (<c>POST https://api.razorpay.com/v1/payments/{payment_id}/refund</c>) before going live.
    /// The <c>RazorpayPaymentId</c> on the order entity contains the payment ID needed.
    /// </remarks>
    /// <param name="id">Order GUID.</param>
    /// <param name="req">Reason for the refund and optional partial refund amount.</param>
    /// <response code="200">Refund processed (or staged for Razorpay).</response>
    /// <response code="400">Order is not in <c>Paid</c> payment status.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessRefund(Guid id, [FromBody] RefundRequest req)
    {
        var order = await _uow.Orders.GetByIdAsync(id);
        if (order is null) return NotFound();
        if (order.PaymentStatus != "Paid")
            return BadRequest(new { message = "Order is not in a paid state" });

        // TODO: Call Razorpay refund API here
        order.PaymentStatus = "Refunded";
        order.Status = "Returned";

        await _uow.OrderStatusHistories.AddAsync(new OrderStatusHistory
        {
            OrderId = id,
            Status = "Returned",
            Comment = $"Refund processed. Reason: {req.Reason}",
            ChangedByUserId = CurrentUserId
        });

        await _uow.Orders.UpdateAsync(order);
        await _uow.SaveChangesAsync();

        return Ok(new { message = "Refund processed" });
    }

    /// <summary>
    /// Returns order count and revenue broken down by status and payment method
    /// for the specified number of past days.
    /// </summary>
    /// <param name="days">Number of past days to analyse. Default: 30.</param>
    /// <response code="200">Status breakdown and payment method breakdown arrays.</response>
    [HttpGet("analytics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderAnalytics([FromQuery] int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var statusBreakdown = await _uow.Orders.Query()
            .Where(o => o.CreatedAt >= since)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.Total) })
            .ToListAsync();

        var paymentMethodBreakdown = await _uow.Orders.Query()
            .Where(o => o.CreatedAt >= since && o.PaymentStatus == "Paid")
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new { Method = g.Key ?? "Unknown", Count = g.Count(), Revenue = g.Sum(o => o.Total) })
            .ToListAsync();

        return Ok(new { statusBreakdown, paymentMethodBreakdown });
    }
}

/// <summary>Request body for processing a refund.</summary>
/// <param name="Reason">Human-readable reason for the refund (shown in order history).</param>
/// <param name="Amount">Optional partial refund amount in INR. If null, a full refund is assumed.</param>
public record RefundRequest(string Reason, decimal? Amount = null);
