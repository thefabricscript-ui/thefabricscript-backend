using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFabricScript.Core.DTOs.Admin;
using TheFabricScript.Core.Interfaces;

namespace TheFabricScript.API.Controllers.Admin;

/// <summary>
/// Admin dashboard analytics and KPI endpoints.
/// All routes require the <c>Admin</c> or <c>SuperAdmin</c> role.
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
[Tags("Admin — Dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the controller with the Unit of Work.</summary>
    public AdminDashboardController(IUnitOfWork uow) => _uow = uow;

    /// <summary>
    /// Returns the full dashboard statistics including revenue, orders, users, products,
    /// daily revenue chart data, order status breakdown, and top-performing products.
    /// </summary>
    /// <remarks>
    /// This is the primary endpoint powering the admin dashboard home page.
    ///
    /// **KPIs returned:**
    /// - Revenue: total, today, this month, last month, month-over-month growth %
    /// - Orders: total, today, this month, by status (pending/processing/shipped/delivered/cancelled)
    /// - Users: total, new today, new this month, active this month (placed ≥1 order)
    /// - Products: total, active, low-stock (≤5 units), out-of-stock
    /// - Average Order Value, Conversion Rate placeholder
    ///
    /// **Charts returned:**
    /// - `revenueChart` — daily revenue + order count for the last N days
    /// - `orderStatusChart` — donut chart data (status → count)
    /// - `topProducts` — top 10 by revenue in the period
    /// - `topViewedProducts` — top 10 by all-time view count
    ///
    /// All monetary values are in **INR (₹)**.
    /// </remarks>
    /// <param name="days">Number of past days to include in chart data. Default: 30.</param>
    /// <response code="200">Full dashboard stats object (<see cref="DashboardStatsDto"/>).</response>
    /// <response code="401">Missing or invalid JWT token.</response>
    /// <response code="403">Authenticated user does not have Admin or SuperAdmin role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard([FromQuery] int days = 30)
    {
        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var rangeStart = now.AddDays(-days);

        var paidOrders = await _uow.Orders.Query()
            .Where(o => o.PaymentStatus == "Paid")
            .ToListAsync();

        var totalRevenue = paidOrders.Sum(o => o.Total);
        var revenueToday = paidOrders.Where(o => o.CreatedAt >= startOfToday).Sum(o => o.Total);
        var revenueThisMonth = paidOrders.Where(o => o.CreatedAt >= startOfMonth).Sum(o => o.Total);
        var revenueLastMonth = paidOrders.Where(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfMonth).Sum(o => o.Total);
        var revenueGrowth = revenueLastMonth == 0 ? 0 : ((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100;

        var allOrders = await _uow.Orders.Query().ToListAsync();
        var allUsers = await _uow.Users.Query().ToListAsync();
        var allProducts = await _uow.Products.Query().ToListAsync();

        var activeUsersThisMonth = await _uow.Orders.Query()
            .Where(o => o.CreatedAt >= startOfMonth)
            .Select(o => o.UserId)
            .Distinct()
            .CountAsync();

        var aov = paidOrders.Count > 0 ? paidOrders.Average(o => o.Total) : 0;

        var revenueChart = paidOrders
            .Where(o => o.CreatedAt >= rangeStart)
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new RevenueChartPoint
            {
                Label = g.Key.ToString("yyyy-MM-dd"),
                Revenue = g.Sum(o => o.Total),
                Orders = g.Count()
            }).ToList();

        var statusChart = allOrders
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatusBreakdown { Status = g.Key, Count = g.Count() })
            .ToList();

        var topProducts = await _uow.OrderItems.Query()
            .Include(oi => oi.Product).ThenInclude(p => p.Images)
            .Where(oi => oi.Order.PaymentStatus == "Paid" && oi.Order.CreatedAt >= rangeStart)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .Select(g => new TopProductDto
            {
                Id = g.Key.ProductId,
                Name = g.Key.ProductName,
                Count = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(t => t.Revenue)
            .Take(10)
            .ToListAsync();

        var topViewed = await _uow.Products.Query()
            .Include(p => p.Images)
            .OrderByDescending(p => p.ViewCount)
            .Take(10)
            .Select(p => new TopProductDto
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)!.Url,
                Count = p.ViewCount,
                Revenue = 0
            })
            .ToListAsync();

        var stats = new DashboardStatsDto
        {
            TotalRevenue = totalRevenue,
            RevenueToday = revenueToday,
            RevenueThisMonth = revenueThisMonth,
            RevenueLastMonth = revenueLastMonth,
            RevenueGrowthPercent = Math.Round(revenueGrowth, 2),
            TotalOrders = allOrders.Count,
            OrdersToday = allOrders.Count(o => o.CreatedAt >= startOfToday),
            OrdersThisMonth = allOrders.Count(o => o.CreatedAt >= startOfMonth),
            PendingOrders = allOrders.Count(o => o.Status == "Pending"),
            ProcessingOrders = allOrders.Count(o => o.Status is "Confirmed" or "Packed"),
            ShippedOrders = allOrders.Count(o => o.Status == "Shipped"),
            DeliveredOrders = allOrders.Count(o => o.Status == "Delivered"),
            CancelledOrders = allOrders.Count(o => o.Status == "Cancelled"),
            TotalUsers = allUsers.Count,
            NewUsersToday = allUsers.Count(u => u.CreatedAt >= startOfToday),
            NewUsersThisMonth = allUsers.Count(u => u.CreatedAt >= startOfMonth),
            ActiveUsersThisMonth = activeUsersThisMonth,
            TotalProducts = allProducts.Count,
            ActiveProducts = allProducts.Count(p => p.IsActive),
            LowStockProducts = allProducts.Count(p => p.Stock is > 0 and <= 5),
            OutOfStockProducts = allProducts.Count(p => p.Stock == 0),
            AverageOrderValue = Math.Round(aov, 2),
            RevenueChart = revenueChart,
            OrderStatusChart = statusChart,
            TopProducts = topProducts,
            TopViewedProducts = topViewed
        };

        return Ok(stats);
    }

    /// <summary>
    /// Returns products that need immediate stock attention — low stock (1–5 units) and out-of-stock.
    /// Intended for the inventory alert widget on the dashboard.
    /// </summary>
    /// <remarks>
    /// Ordered by stock ascending so the most critical items appear first.
    /// Each item includes: id, name, SKU, current stock, category, primary image URL, and alert level.
    ///
    /// **Alert levels:**
    /// - `"Out of Stock"` — stock = 0
    /// - `"Low Stock"` — stock between 1 and 5 inclusive
    /// </remarks>
    /// <response code="200">List of products requiring stock attention.</response>
    [HttpGet("inventory-alerts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryAlerts()
    {
        var alerts = await _uow.Products.Query()
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Stock <= 5)
            .OrderBy(p => p.Stock)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.SKU,
                p.Stock,
                CategoryName = p.Category.Name,
                PrimaryImage = p.Images.FirstOrDefault(i => i.IsPrimary)!.Url,
                Alert = p.Stock == 0 ? "Out of Stock" : "Low Stock"
            })
            .ToListAsync();

        return Ok(alerts);
    }
}
