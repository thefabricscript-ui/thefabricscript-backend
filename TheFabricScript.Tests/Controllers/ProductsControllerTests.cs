using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TheFabricScript.API.Controllers;
using TheFabricScript.Core.Entities;
using TheFabricScript.Infrastructure.Repositories;
using TheFabricScript.Tests.Helpers;
using Xunit;

namespace TheFabricScript.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="ProductsController"/>.
/// Uses a real <see cref="UnitOfWork"/> backed by the EF Core InMemory provider,
/// so no mocking of repositories is needed — the tests exercise the actual query logic.
/// </summary>
public class ProductsControllerTests
{
    // ── GetProducts ───────────────────────────────────────

    /// <summary>
    /// GetProducts with no filters should return all active products.
    /// </summary>
    [Fact]
    public async Task GetProducts_ShouldReturnAllActiveProducts_WhenNoFiltersApplied()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new ProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.GetProducts(null, null, null, null, null) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        var body = result.Value as dynamic;
        // Both seeded products are active — expect 2
        ((int)body!.total).Should().Be(2);
    }

    /// <summary>
    /// GetProducts filtered by category slug should only return products in that category.
    /// </summary>
    [Fact]
    public async Task GetProducts_ShouldFilterByCategory_WhenCategorySlugProvided()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);

        // Add a product in a different category to ensure filtering works
        var otherCategory = new Category { Name = "Men's Wear", Slug = "mens-wear", IsActive = true };
        var otherProduct = new Product
        {
            Name = "Men's Shirt",
            Slug = "mens-shirt",
            BasePrice = 799,
            CategoryId = otherCategory.Id,
            IsActive = true
        };
        context.Categories.Add(otherCategory);
        context.Products.Add(otherProduct);
        await context.SaveChangesAsync();

        var controller = new ProductsController(new UnitOfWork(context));

        // Act — filter to womens-wear only
        var result = await controller.GetProducts("womens-wear", null, null, null, null) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var body = result!.Value as dynamic;
        ((int)body!.total).Should().Be(2, "only the 2 women's wear products should match");
    }

    /// <summary>
    /// GetProducts with minPrice filter should exclude products below the threshold.
    /// </summary>
    [Fact]
    public async Task GetProducts_ShouldFilterByMinPrice_WhenMinPriceProvided()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new ProductsController(new UnitOfWork(context));

        // Act — only products with BasePrice >= 2000 (Silk Saree at 3499 should pass)
        var result = await controller.GetProducts(null, 2000m, null, null, null) as OkObjectResult;

        // Assert
        var body = result!.Value as dynamic;
        ((int)body!.total).Should().Be(1);
    }

    /// <summary>
    /// GetProducts with price_asc sort should return cheapest products first.
    /// </summary>
    [Fact]
    public async Task GetProducts_ShouldSortByPriceAscending_WhenPriceAscRequested()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new ProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.GetProducts(null, null, null, null, null, sort: "price_asc") as OkObjectResult;
        var body = result!.Value as dynamic;
        var data = body!.data as IEnumerable<Product>;

        // Assert — Kurta (999 discounted) should come before Saree (3499)
        data!.First().Name.Should().Be("Floral Cotton Kurta");
    }

    // ── GetProduct (by slug) ──────────────────────────────

    /// <summary>
    /// GetProduct should return 200 with full product details for a valid slug.
    /// </summary>
    [Fact]
    public async Task GetProduct_ShouldReturn200_WhenSlugExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new ProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.GetProduct("floral-cotton-kurta") as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var product = result.Value as Product;
        product!.Slug.Should().Be("floral-cotton-kurta");
    }

    /// <summary>
    /// GetProduct should return 404 when no active product matches the slug.
    /// </summary>
    [Fact]
    public async Task GetProduct_ShouldReturn404_WhenSlugNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new ProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.GetProduct("non-existent-slug");

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Calling GetProduct should increment the product's ViewCount by 1.
    /// </summary>
    [Fact]
    public async Task GetProduct_ShouldIncrementViewCount_OnEachCall()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var initialViewCount = SeedData.Product1().ViewCount; // 120
        var uow = new UnitOfWork(context);
        var controller = new ProductsController(uow);

        // Act — call twice
        await controller.GetProduct("floral-cotton-kurta");
        await controller.GetProduct("floral-cotton-kurta");

        // Assert
        var product = await uow.Products.GetByIdAsync(SeedData.ProductId1);
        product!.ViewCount.Should().Be(initialViewCount + 2);
    }

    // ── Search ────────────────────────────────────────────

    /// <summary>
    /// Search with a matching term should return relevant products.
    /// </summary>
    [Fact]
    public async Task Search_ShouldReturnMatchingProducts_WhenQueryMatchesName()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new ProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.Search("kurta") as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var body = result!.Value as dynamic;
        ((int)body!.total).Should().Be(1);
    }

    /// <summary>
    /// Search with an empty query should return 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task Search_ShouldReturn400_WhenQueryIsEmpty()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var controller = new ProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.Search("   ");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Search with a term that matches no products should return an empty list (not 404).
    /// </summary>
    [Fact]
    public async Task Search_ShouldReturnEmptyList_WhenNoProductsMatch()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new ProductsController(new UnitOfWork(context));

        // Act
        var result = await controller.Search("xyznonexistent") as OkObjectResult;

        // Assert
        var body = result!.Value as dynamic;
        ((int)body!.total).Should().Be(0);
    }
}
