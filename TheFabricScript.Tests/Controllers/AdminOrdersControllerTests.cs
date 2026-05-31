using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TheFabricScript.API.Controllers.Admin;
using TheFabricScript.Core.DTOs.Admin;
using TheFabricScript.Core.Entities;
using TheFabricScript.Infrastructure.Repositories;
using TheFabricScript.Tests.Helpers;
using Xunit;

namespace TheFabricScript.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="AdminOrdersController"/>.
/// Covers order listing, status transitions, shipment updates, and refund processing.
/// </summary>
public class AdminOrdersControllerTests
{
    /// <summary>
    /// Helper that wires a fake JWT <see cref="ClaimsPrincipal"/> onto the controller's
    /// <see cref="ControllerBase.HttpContext"/> so that <c>User.FindFirst("sub")</c>
    /// returns the admin user ID during tests that need it.
    /// </summary>
    private static AdminOrdersController CreateController(UnitOfWork uow)
    {
        var controller = new AdminOrdersController(uow);
        var claims = new[] { new Claim("sub", SeedData.AdminUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return controller;
    }

    private static async Task<(UnitOfWork uow, Guid addressId)> SeedWithOrderAsync()
    {
        var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);

        // Address required by Order FK
        var address = new Address
        {
            UserId = SeedData.UserId1,
            FullName = "Test Customer",
            Phone = "+919999999999",
            Line1 = "123 Main St",
            City = "Chennai",
            State = "Tamil Nadu",
            Pincode = "600001"
        };
        context.Addresses.Add(address);
        context.Orders.Add(SeedData.PaidOrder(address.Id));
        await context.SaveChangesAsync();
        return (new UnitOfWork(context), address.Id);
    }

    // ── GetOrders ─────────────────────────────────────────

    /// <summary>
    /// GetOrders should return all orders with no filters applied.
    /// </summary>
    [Fact]
    public async Task GetOrders_ShouldReturnAllOrders_WhenNoFiltersApplied()
    {
        // Arrange
        var (uow, _) = await SeedWithOrderAsync();
        var controller = CreateController(uow);

        // Act
        var result = await controller.GetOrders(null, null, null, null, null) as OkObjectResult;
        var body = result!.Value as dynamic;

        // Assert
        ((int)body!.total).Should().Be(1);
    }

    /// <summary>
    /// GetOrders filtered by status should return only matching orders.
    /// </summary>
    [Fact]
    public async Task GetOrders_ShouldFilterByStatus_WhenStatusProvided()
    {
        // Arrange
        var (uow, _) = await SeedWithOrderAsync();
        var controller = CreateController(uow);

        // Act — filter by Delivered (matches seed order)
        var deliveredResult = await controller.GetOrders("Delivered", null, null, null, null) as OkObjectResult;
        var body = deliveredResult!.Value as dynamic;
        ((int)body!.total).Should().Be(1);

        // Act — filter by Pending (no matches)
        var pendingResult = await controller.GetOrders("Pending", null, null, null, null) as OkObjectResult;
        var pendingBody = pendingResult!.Value as dynamic;
        ((int)pendingBody!.total).Should().Be(0);
    }

    // ── UpdateStatus ──────────────────────────────────────

    /// <summary>
    /// UpdateStatus with a valid status should update the order and append a history record.
    /// </summary>
    [Fact]
    public async Task UpdateStatus_ShouldUpdateOrderStatusAndCreateHistory()
    {
        // Arrange
        var (uow, _) = await SeedWithOrderAsync();
        var controller = CreateController(uow);

        var dto = new UpdateOrderStatusDto { Status = "Returned", Comment = "Customer returned the item" };

        // Act
        var result = await controller.UpdateStatus(SeedData.OrderId1, dto) as OkObjectResult;
        var body = result!.Value as dynamic;

        // Assert — response
        ((string)body!.newStatus).Should().Be("Returned");

        // Assert — history record created
        var historyCount = await uow.OrderStatusHistories.CountAsync(h => h.OrderId == SeedData.OrderId1);
        historyCount.Should().Be(1);
    }

    /// <summary>
    /// UpdateStatus with an invalid status value should return 400.
    /// </summary>
    [Fact]
    public async Task UpdateStatus_ShouldReturn400_WhenStatusIsInvalid()
    {
        // Arrange
        var (uow, _) = await SeedWithOrderAsync();
        var controller = CreateController(uow);

        var dto = new UpdateOrderStatusDto { Status = "InvalidStatus" };

        // Act
        var result = await controller.UpdateStatus(SeedData.OrderId1, dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// UpdateStatus should set DeliveredAt when status is changed to Delivered.
    /// </summary>
    [Fact]
    public async Task UpdateStatus_ShouldSetDeliveredAt_WhenStatusIsDelivered()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var address = new Address
        {
            UserId = SeedData.UserId1, FullName = "Test", Phone = "+91000",
            Line1 = "123", City = "City", State = "State", Pincode = "000000"
        };
        // Create a non-delivered order
        var order = new Order
        {
            OrderNumber = "TFS-TEST-0002",
            UserId = SeedData.UserId1,
            ShippingAddressId = address.Id,
            Status = "Shipped",
            PaymentStatus = "Paid",
            Total = 999
        };
        context.Addresses.Add(address);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var controller = CreateController(uow);

        // Act
        await controller.UpdateStatus(order.Id, new UpdateOrderStatusDto { Status = "Delivered" });

        // Assert
        var updated = await uow.Orders.GetByIdAsync(order.Id);
        updated!.DeliveredAt.Should().NotBeNull("DeliveredAt must be set automatically");
        updated.DeliveredAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── ProcessRefund ─────────────────────────────────────

    /// <summary>
    /// ProcessRefund should update payment and order status for a paid order.
    /// </summary>
    [Fact]
    public async Task ProcessRefund_ShouldMarkOrderAsRefunded_WhenOrderIsPaid()
    {
        // Arrange
        var (uow, _) = await SeedWithOrderAsync();
        var controller = CreateController(uow);

        // Act
        var result = await controller.ProcessRefund(SeedData.OrderId1, new RefundRequest("Product damaged"));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var updated = await uow.Orders.GetByIdAsync(SeedData.OrderId1);
        updated!.PaymentStatus.Should().Be("Refunded");
        updated.Status.Should().Be("Returned");
    }

    /// <summary>
    /// ProcessRefund should return 400 when the order is not in Paid status.
    /// </summary>
    [Fact]
    public async Task ProcessRefund_ShouldReturn400_WhenOrderIsNotPaid()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var address = new Address
        {
            UserId = SeedData.UserId1, FullName = "T", Phone = "+91000",
            Line1 = "1", City = "C", State = "S", Pincode = "000000"
        };
        var unpaidOrder = new Order
        {
            OrderNumber = "TFS-UNPAID-001",
            UserId = SeedData.UserId1,
            ShippingAddressId = address.Id,
            Status = "Pending",
            PaymentStatus = "Pending", // not paid
            Total = 500
        };
        context.Addresses.Add(address);
        context.Orders.Add(unpaidOrder);
        await context.SaveChangesAsync();

        var controller = CreateController(new UnitOfWork(context));

        // Act
        var result = await controller.ProcessRefund(unpaidOrder.Id, new RefundRequest("Test"));

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
