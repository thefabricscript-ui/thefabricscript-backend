namespace TheFabricScript.Core.Entities;

public class UserCoupon : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CouponId { get; set; }
    public int UsedCount { get; set; } = 0;

    // Navigation
    public User User { get; set; } = null!;
    public Coupon Coupon { get; set; } = null!;
}
