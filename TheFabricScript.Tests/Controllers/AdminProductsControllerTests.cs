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
/// Unit tests for <see cref="AdminProductsController"/>.
/// Covers CRUD operations, slug uniqueness enforcement, stock updates,
/// and soft-delete behaviour.
/// </summary>
public class AdminProductsControllerTests
{
    // ── GetProducts ───────────────────────────────────────

    /// <summary>
    /// Admin GetProducts uses IgnoreQueryFilters — it should include inactive products
    /// that the public endpoint hides.
    /// </summary>
    [Fact]
    public async Task GetProducts_ShouldIncludeInactiveProducts_ForAdmin()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);

        // Add an inactive product
        context.Products.Add(new Product
        {
            Name = "Hidden Product",
            Slug = "hidden-product",
            BasePrice = 499,
            CategoryId = SeedData.CategoryId1,
            IsActive = false
        });
        await context.SaveChangesAsync();

        var controller = new AdminProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.GetProducts(null, null, null, null) as OkObjectResult;
        var body = result!.Value as dynamic;

        // Assert — 3 products: 2 seeded + 1 inactive
        ((int)body!.total).Should().Be(3);
    }

    /// <summary>
    /// GetProducts with stockAlert="out" should only return out-of-stock products.
    /// </summary>
    [Fact]
    public async Task GetProducts_ShouldFilterOutOfStockOnly_WhenStockAlertIsOut()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new AdminProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.GetProducts(null, null, null, "out") as OkObjectResult;
        var body = result!.Value as dynamic;

        // Assert — only Product2 (Silk Saree) has stock = 0
        ((int)body!.total).Should().Be(1);
    }

    // ── CreateProduct ─────────────────────────────────────

    /// <summary>
    /// CreateProduct should return 201 Created with the new product when valid data is provided.
    /// </summary>
    [Fact]
    public async Task CreateProduct_ShouldReturn201_WhenProductIsValid()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new AdminProductsController(new UnitOfWork(context));

        var dto = new CreateProductDto
        {
            Name = "New Kurta",
            Slug = "new-kurta",
            BasePrice = 899,
            Stock = 30,
            SKU = "TFS-NEW-001",
            CategoryId = SeedData.CategoryId1
        };

        // Act
        var result = await controller.CreateProduct(dto) as CreatedAtActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        var created = result.Value as Product;
        created!.Name.Should().Be("New Kurta");
        created.IsActive.Should().BeTrue("new products should default to active");
    }

    /// <summary>
    /// CreateProduct should return 409 Conflict when the provided slug is already in use.
    /// </summary>
    [Fact]
    public async Task CreateProduct_ShouldReturn409_WhenSlugAlreadyExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new AdminProductsController(new UnitOfWork(context));

        var dto = new CreateProductDto
        {
            Name = "Duplicate Kurta",
            Slug = "floral-cotton-kurta", // already exists in seed data
            BasePrice = 500,
            CategoryId = SeedData.CategoryId1
        };

        // Act
        var result = await controller.CreateProduct(dto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// CreateProduct with variants should persist all variants linked to the product.
    /// </summary>
    [Fact]
    public async Task CreateProduct_ShouldPersistVariants_WhenVariantsIncluded()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var uow = new UnitOfWork(context);
        var controller = new AdminProductsController(uow);

        var dto = new CreateProductDto
        {
            Name = "Multi-Variant Kurta",
            Slug = "multi-variant-kurta",
            BasePrice = 999,
            CategoryId = SeedData.CategoryId1,
            Variants = new List<CreateVariantDto>
            {
                new() { Size = "S", Color = "Blue", Stock = 10 },
                new() { Size = "M", Color = "Blue", Stock = 15 },
                new() { Size = "L", Color = "Red",  Stock = 5  }
            }
        };

        // Act
        var result = await controller.CreateProduct(dto) as CreatedAtActionResult;
        var product = result!.Value as Product;

        // Assert
        product!.Variants.Should().HaveCount(3);
        product.Variants.Should().Contain(v => v.Size == "S" && v.Color == "Blue");
    }

    // ── UpdateProduct ─────────────────────────────────────

    /// <summary>
    /// UpdateProduct should return 200 OK with the updated values.
    /// </summary>
    [Fact]
    public async Task UpdateProduct_ShouldReturn200_WhenProductExistsAndDataIsValid()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new AdminProductsController(new UnitOfWork(context));

        var dto = new UpdateProductDto
        {
            Name = "Updated Kurta",
            Slug = "updated-kurta-slug",
            BasePrice = 1199,
            Stock = 40,
            CategoryId = SeedData.CategoryId1,
            IsActive = true
        };

        // Act
        var result = await controller.UpdateProduct(SeedData.ProductId1, dto) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var updated = result!.Value as Product;
        updated!.Name.Should().Be("Updated Kurta");
        updated.BasePrice.Should().Be(1199);
    }

    /// <summary>
    /// UpdateProduct should return 404 when the product ID does not exist.
    /// </summary>
    [Fact]
    public async Task UpdateProduct_ShouldReturn404_WhenProductNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var controller = new AdminProductsController(new UnitOfWork(context));

        var dto = new UpdateProductDto
        {
            Name = "Ghost Product",
            Slug = "ghost",
            BasePrice = 100,
            CategoryId = Guid.NewGuid(),
            IsActive = true
        };

        // Act
        var result = await controller.UpdateProduct(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    // ── ToggleActive ──────────────────────────────────────

    /// <summary>
    /// ToggleActive should flip IsActive from true to false (and back).
    /// </summary>
    [Fact]
    public async Task ToggleActive_ShouldFlipIsActiveFlag()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var uow = new UnitOfWork(context);
        var controller = new AdminProductsController(uow);

        // Product1 starts as active
        // Act — toggle once (active → inactive)
        var result1 = await controller.ToggleActive(SeedData.ProductId1) as OkObjectResult;
        var body1 = result1!.Value as dynamic;
        ((bool)body1!.isActive).Should().BeFalse();

        // Act — toggle again (inactive → active)
        var result2 = await controller.ToggleActive(SeedData.ProductId1) as OkObjectResult;
        var body2 = result2!.Value as dynamic;
        ((bool)body2!.isActive).Should().BeTrue();
    }

    // ── UpdateStock ───────────────────────────────────────

    /// <summary>
    /// UpdateStock should set the product's stock to the new value.
    /// </summary>
    [Fact]
    public async Task UpdateStock_ShouldUpdateProductStock_WhenNoVariantSpecified()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var uow = new UnitOfWork(context);
        var controller = new AdminProductsController(uow);

        // Act
        await controller.UpdateStock(SeedData.ProductId1, new UpdateStockRequest(75));

        // Assert
        var product = await uow.Products.GetByIdAsync(SeedData.ProductId1);
        product!.Stock.Should().Be(75);
    }
}
