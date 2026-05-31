using TheFabricScript.Core.Entities;
using TheFabricScript.Infrastructure.Data;

namespace TheFabricScript.Tests.Helpers;

/// <summary>
/// Centralised seed data factory for unit tests.
/// Provides pre-built entity instances and helper methods for seeding
/// an <see cref="AppDbContext"/> with realistic test data.
/// </summary>
public static class SeedData
{
    // ── Fixed GUIDs ───────────────────────────────────────
    // Using fixed GUIDs makes assertions readable and deterministic.

    /// <summary>GUID used for the primary test customer user.</summary>
    public static readonly Guid UserId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>GUID used for the admin test user.</summary>
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    /// <summary>GUID used for the primary test category.</summary>
    public static readonly Guid CategoryId1 = Guid.Parse("00000000-0000-0000-0000-000000000010");

    /// <summary>GUID used for the secondary test category.</summary>
    public static readonly Guid CategoryId2 = Guid.Parse("00000000-0000-0000-0000-000000000011");

    /// <summary>GUID used for the first test product.</summary>
    public static readonly Guid ProductId1 = Guid.Parse("00000000-0000-0000-0000-000000000020");

    /// <summary>GUID used for the second test product.</summary>
    public static readonly Guid ProductId2 = Guid.Parse("00000000-0000-0000-0000-000000000021");

    /// <summary>GUID used for the first test order.</summary>
    public static readonly Guid OrderId1 = Guid.Parse("00000000-0000-0000-0000-000000000030");

    /// <summary>GUID used for the first test coupon.</summary>
    public static readonly Guid CouponId1 = Guid.Parse("00000000-0000-0000-0000-000000000040");

    // ── Entity Factories ──────────────────────────────────

    /// <summary>Returns a standard customer <see cref="User"/> ready for seeding.</summary>
    public static User CustomerUser() => new()
    {
        Id = UserId1,
        FirstName = "Test",
        LastName = "Customer",
        Email = "customer@test.com",
        Phone = "+919999999999",
        Role = "Customer",
        IsActive = true,
        IsEmailVerified = true,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234")
    };

    /// <summary>Returns an admin <see cref="User"/> ready for seeding.</summary>
    public static User AdminUser() => new()
    {
        Id = AdminUserId,
        FirstName = "Admin",
        LastName = "User",
        Email = "admin@thefabricscript.com",
        Role = "Admin",
        IsActive = true,
        IsEmailVerified = true,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234")
    };

    /// <summary>Returns a top-level <see cref="Category"/> (e.g. Women's Wear).</summary>
    public static Category Category1() => new()
    {
        Id = CategoryId1,
        Name = "Women's Wear",
        Slug = "womens-wear",
        IsActive = true,
        SortOrder = 1
    };

    /// <summary>Returns a child <see cref="Category"/> under <see cref="Category1"/>.</summary>
    public static Category Category2() => new()
    {
        Id = CategoryId2,
        Name = "Sarees",
        Slug = "sarees",
        ParentId = CategoryId1,
        IsActive = true,
        SortOrder = 1
    };

    /// <summary>Returns a fully-populated active <see cref="Product"/> for seeding.</summary>
    public static Product Product1() => new()
    {
        Id = ProductId1,
        Name = "Floral Cotton Kurta",
        Slug = "floral-cotton-kurta",
        Description = "Beautiful floral printed cotton kurta.",
        BasePrice = 1299,
        DiscountPrice = 999,
        Stock = 50,
        SKU = "TFS-KURTA-001",
        Brand = "The Fabric Script",
        CategoryId = CategoryId1,
        IsActive = true,
        IsFeatured = true,
        ViewCount = 120
    };

    /// <summary>Returns a second active product (out-of-stock).</summary>
    public static Product Product2() => new()
    {
        Id = ProductId2,
        Name = "Silk Blend Saree",
        Slug = "silk-blend-saree",
        BasePrice = 3499,
        Stock = 0,
        SKU = "TFS-SAREE-001",
        Brand = "The Fabric Script",
        CategoryId = CategoryId1,
        IsActive = true
    };

    /// <summary>Returns a percentage-based <see cref="Coupon"/> (10% off, min order ₹500).</summary>
    public static Coupon PercentageCoupon() => new()
    {
        Id = CouponId1,
        Code = "WELCOME10",
        Description = "10% off on orders above ₹500",
        DiscountType = "Percentage",
        DiscountValue = 10,
        MinOrderAmount = 500,
        MaxDiscountAmount = 200,
        MaxUses = 100,
        UsedCount = 5,
        IsActive = true,
        ExpiresAt = DateTime.UtcNow.AddDays(30)
    };

    /// <summary>Returns a paid <see cref="Order"/> for the customer user.</summary>
    public static Order PaidOrder(Guid addressId) => new()
    {
        Id = OrderId1,
        OrderNumber = "TFS-2026-0001",
        UserId = UserId1,
        ShippingAddressId = addressId,
        Status = "Delivered",
        PaymentStatus = "Paid",
        PaymentMethod = "Razorpay",
        Subtotal = 999,
        Total = 999,
        DeliveredAt = DateTime.UtcNow.AddDays(-2)
    };

    // ── Seed Helpers ──────────────────────────────────────

    /// <summary>
    /// Seeds a minimal dataset (users + categories + products) into <paramref name="context"/>.
    /// Call <c>await context.SaveChangesAsync()</c> before using the data in assertions
    /// if your test method does not also call <see cref="AppDbContext.SaveChangesAsync"/>.
    /// </summary>
    public static async Task SeedBasicAsync(AppDbContext context)
    {
        context.Users.AddRange(CustomerUser(), AdminUser());
        context.Categories.AddRange(Category1(), Category2());
        context.Products.AddRange(Product1(), Product2());
        await context.SaveChangesAsync();
    }
}
