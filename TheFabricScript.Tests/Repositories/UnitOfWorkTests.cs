using FluentAssertions;
using TheFabricScript.Core.Entities;
using TheFabricScript.Infrastructure.Repositories;
using TheFabricScript.Tests.Helpers;
using Xunit;

namespace TheFabricScript.Tests.Repositories;

/// <summary>
/// Unit tests verifying the atomic commit behaviour of <see cref="UnitOfWork"/>.
/// Ensures that multiple repository operations are committed together in
/// a single <see cref="UnitOfWork.SaveChangesAsync"/> call.
/// </summary>
public class UnitOfWorkTests
{
    /// <summary>
    /// Adding records to two different repositories and calling SaveChangesAsync once
    /// should persist both records atomically.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ShouldCommitMultipleRepositoryChanges_Atomically()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var uow = new UnitOfWork(context);

        var category = new Category { Name = "New Category", Slug = "new-cat", IsActive = true };
        var product = new Product
        {
            Name = "New Product",
            Slug = "new-product",
            BasePrice = 999,
            CategoryId = category.Id,
            IsActive = true
        };

        // Act — stage both changes, commit once
        await uow.Categories.AddAsync(category);
        await uow.Products.AddAsync(product);
        var rowsAffected = await uow.SaveChangesAsync();

        // Assert — both saved in one commit
        rowsAffected.Should().Be(2);

        var savedCategory = await uow.Categories.GetByIdAsync(category.Id);
        var savedProduct = await uow.Products.GetByIdAsync(product.Id);

        savedCategory.Should().NotBeNull();
        savedProduct.Should().NotBeNull();
    }

    /// <summary>
    /// SaveChangesAsync should automatically set UpdatedAt on modified entities.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ShouldUpdateTimestamp_WhenEntityIsModified()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var uow = new UnitOfWork(context);

        var product = await uow.Products.GetByIdAsync(SeedData.ProductId1);
        var originalUpdatedAt = product!.UpdatedAt;

        // Ensure some time passes (InMemory is fast — force the difference)
        await Task.Delay(10);
        product.Name = "Modified Name";

        // Act
        await uow.Products.UpdateAsync(product);
        await uow.SaveChangesAsync();

        // Assert
        var updated = await uow.Products.GetByIdAsync(SeedData.ProductId1);
        updated!.UpdatedAt.Should().BeAfter(originalUpdatedAt,
            "UpdatedAt must be refreshed on every save");
    }

    /// <summary>
    /// All repository properties on UnitOfWork should be initialised (non-null)
    /// immediately after construction.
    /// </summary>
    [Fact]
    public void UnitOfWork_ShouldInitialiseAllRepositories_OnConstruction()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        // Act
        var uow = new UnitOfWork(context);

        // Assert — none should be null
        uow.Users.Should().NotBeNull();
        uow.Products.Should().NotBeNull();
        uow.ProductVariants.Should().NotBeNull();
        uow.ProductImages.Should().NotBeNull();
        uow.Categories.Should().NotBeNull();
        uow.Orders.Should().NotBeNull();
        uow.OrderItems.Should().NotBeNull();
        uow.OrderStatusHistories.Should().NotBeNull();
        uow.Addresses.Should().NotBeNull();
        uow.CartItems.Should().NotBeNull();
        uow.WishlistItems.Should().NotBeNull();
        uow.Reviews.Should().NotBeNull();
        uow.Coupons.Should().NotBeNull();
        uow.UserCoupons.Should().NotBeNull();
        uow.Shipments.Should().NotBeNull();
        uow.AuditLogs.Should().NotBeNull();
    }
}
