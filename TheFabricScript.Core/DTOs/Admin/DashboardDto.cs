namespace TheFabricScript.Core.DTOs.Admin;

public class DashboardStatsDto
{
    // Revenue
    public decimal TotalRevenue { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public decimal RevenueGrowthPercent { get; set; }

    // Orders
    public int TotalOrders { get; set; }
    public int OrdersToday { get; set; }
    public int OrdersThisMonth { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }

    // Users
    public int TotalUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int ActiveUsersThisMonth { get; set; }

    // Products
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockProducts { get; set; }   // stock <= 5
    public int OutOfStockProducts { get; set; }

    // Conversion
    public decimal ConversionRate { get; set; }
    public decimal AverageOrderValue { get; set; }

    // Charts
    public List<RevenueChartPoint> RevenueChart { get; set; } = new();
    public List<OrderStatusBreakdown> OrderStatusChart { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopProductDto> TopViewedProducts { get; set; } = new();
}

public class RevenueChartPoint
{
    public string Label { get; set; } = string.Empty; // "2026-05-01"
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
}

public class OrderStatusBreakdown
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Count { get; set; }      // views or units sold
    public decimal Revenue { get; set; }
}
