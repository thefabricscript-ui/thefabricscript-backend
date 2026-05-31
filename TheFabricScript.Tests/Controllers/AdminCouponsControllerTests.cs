using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TheFabricScript.API.Controllers.Admin;
using TheFabricScript.Core.DTOs.Admin;
using TheFabricScript.Core.Entities;
using TheFabricScript.Infrastructure.Repositories;
using TheFabricScript.Tests.Helpers;
using Xunit;

namespace TheFabricScript.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="AdminCouponsController"/>.
/// Covers coupon creation, conflict detection, toggling, and the inline validation logic.
/// </summary>
public class AdminCouponsControllerTests
{
    // ── CreateCoupon ──────────────────────────────────────

    /// <summary>
    /// CreateCoupon should return 201 with the new coupon when the code is unique.
    /// </summary>
    [Fact]
    public async Task CreateCoupon_ShouldReturn201_WhenCodeIsUnique()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var controller = new AdminCouponsController(new UnitOfWork(context));

        var dto = new CreateCouponDto
        {
            Code = "NEWCODE10",
            Description = "10% off",
            DiscountType = "Percentage",
            DiscountValue = 10,
            MinOrderAmount = 500,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await controller.CreateCoupon(dto) as CreatedAtActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        var coupon = result.Value as Coupon;
        coupon!.Code.Should().Be("NEWCODE10");
        coupon.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Coupon codes should be stored in uppercase regardless of the case provided.
    /// </summary>
    [Fact]
    public async Task CreateCoupon_ShouldUppercaseCode_Regardless()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var controller = new AdminCouponsController(new UnitOfWork(context));

        var dto = new CreateCouponDto
        {
            Code = "lowercase10",
            Description = "Test",
            DiscountType = "Flat",
            DiscountValue = 100
        };

        // Act
        var result = await controller.CreateCoupon(dto) as CreatedAtActionResult;
        var coupon = result!.Value as Coupon;

        // Assert
        coupon!.Code.Should().Be("LOWERCASE10");
    }

    /// <summary>
    /// Creating a coupon with a duplicate code should return 409 Conflict.
    /// </summary>
    [Fact]
    public async Task CreateCoupon_ShouldReturn409_WhenCodeAlreadyExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Coupons.Add(SeedData.PercentageCoupon());
        await context.SaveChangesAsync();

        var controller = new AdminCouponsController(new UnitOfWork(context));

        var dto = new CreateCouponDto
        {
            Code = "WELCOME10", // duplicate
            Description = "Duplicate",
            DiscountType = "Percentage",
            DiscountValue = 5
        };

        // Act
        var result = await controller.CreateCoupon(dto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── ToggleActive ──────────────────────────────────────

    /// <summary>
    /// ToggleActive should disable an active coupon and re-enable it on the next call.
    /// </summary>
    [Fact]
    public async Task ToggleActive_ShouldFlipIsActiveFlag_OnEachCall()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Coupons.Add(SeedData.PercentageCoupon());
        await context.SaveChangesAsync();

        var controller = new AdminCouponsController(new UnitOfWork(context));

        // Act — disable
        var result1 = await controller.ToggleActive(SeedData.CouponId1) as OkObjectResult;
        ((bool)(result1!.Value as dynamic)!.isActive).Should().BeFalse();

        // Act — re-enable
        var result2 = await controller.ToggleActive(SeedData.CouponId1) as OkObjectResult;
        ((bool)(result2!.Value as dynamic)!.isActive).Should().BeTrue();
    }

    // ── ValidateCoupon ────────────────────────────────────

    /// <summary>
    /// ValidateCoupon should return the correct discount amount for a valid percentage coupon.
    /// </summary>
    [Fact]
    public async Task ValidateCoupon_ShouldReturnCorrectDiscount_ForPercentageCoupon()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Coupons.Add(SeedData.PercentageCoupon()); // 10% off, max ₹200, min ₹500
        await context.SaveChangesAsync();

        var controller = new AdminCouponsController(new UnitOfWork(context));
        // Set up a fake user context (ValidateCoupon requires [Authorize])
        // For unit tests we call the method directly — auth is enforced at the middleware level
        var request = new ValidateCouponRequest("WELCOME10", 1000m); // 10% of 1000 = 100

        // Act
        var result = await controller.ValidateCoupon(request) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var body = result!.Value as dynamic;
        ((bool)body!.valid).Should().BeTrue();
        ((decimal)body!.DiscountAmount).Should().Be(100m);
        ((decimal)body!.FinalTotal).Should().Be(900m);
    }

    /// <summary>
    /// ValidateCoupon should cap the discount at MaxDiscountAmount.
    /// </summary>
    [Fact]
    public async Task ValidateCoupon_ShouldCapDiscount_AtMaxDiscountAmount()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Coupons.Add(SeedData.PercentageCoupon()); // 10% off, max ₹200
        await context.SaveChangesAsync();

        var controller = new AdminCouponsController(new UnitOfWork(context));
        var request = new ValidateCouponRequest("WELCOME10", 5000m); // 10% = 500, but capped at 200

        // Act
        var result = await controller.ValidateCoupon(request) as OkObjectResult;
        var body = result!.Value as dynamic;

        // Assert
        ((decimal)body!.DiscountAmount).Should().Be(200m, "discount must not exceed MaxDiscountAmount");
        ((decimal)body!.FinalTotal).Should().Be(4800m);
    }

    /// <summary>
    /// ValidateCoupon should return 400 when the order total is below MinOrderAmount.
    /// </summary>
    [Fact]
    public async Task ValidateCoupon_ShouldReturn400_WhenOrderBelowMinAmount()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.Coupons.Add(SeedData.PercentageCoupon()); // min order ₹500
        await context.SaveChangesAsync();

        var controller = new AdminCouponsController(new UnitOfWork(context));
        var request = new ValidateCouponRequest("WELCOME10", 300m); // below min

        // Act
        var result = await controller.ValidateCoupon(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// ValidateCoupon should return 400 for an invalid or unknown coupon code.
    /// </summary>
    [Fact]
    public async Task ValidateCoupon_ShouldReturn400_WhenCouponCodeIsInvalid()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var controller = new AdminCouponsController(new UnitOfWork(context));

        var request = new ValidateCouponRequest("FAKECODE", 1000m);

        // Act
        var result = await controller.ValidateCoupon(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// ValidateCoupon should return 400 if the coupon usage limit has been reached.
    /// </summary>
    [Fact]
    public async Task ValidateCoupon_ShouldReturn400_WhenUsageLimitReached()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var maxedCoupon = new Coupon
        {
            Code = "MAXED",
            Description = "Used up",
            DiscountType = "Flat",
            DiscountValue = 50,
            MaxUses = 10,
            UsedCount = 10, // fully used
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        context.Coupons.Add(maxedCoupon);
        await context.SaveChangesAsync();

        var controller = new AdminCouponsController(new UnitOfWork(context));

        // Act
        var result = await controller.ValidateCoupon(new ValidateCouponRequest("MAXED", 1000m));

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
