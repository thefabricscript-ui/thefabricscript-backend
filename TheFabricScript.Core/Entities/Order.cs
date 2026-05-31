namespace TheFabricScript.Core.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid ShippingAddressId { get; set; }
    public string Status { get; set; } = "Pending";
    // Pending | Confirmed | Packed | Shipped | Delivered | Cancelled | ReturnRequested | Returned

    public decimal Subtotal { get; set; }
    public decimal ShippingCharge { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal Total { get; set; }

    public string PaymentStatus { get; set; } = "Pending";
    // Pending | Paid | Failed | Refunded
    public string? PaymentMethod { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }

    public Guid? CouponId { get; set; }
    public string? Notes { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Address ShippingAddress { get; set; } = null!;
    public Coupon? Coupon { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Shipment? Shipment { get; set; }
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
