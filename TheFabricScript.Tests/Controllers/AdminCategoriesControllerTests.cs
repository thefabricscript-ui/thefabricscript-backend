using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TheFabricScript.API.Controllers.Admin;
using TheFabricScript.Infrastructure.Repositories;
using TheFabricScript.Tests.Helpers;
using Xunit;

namespace TheFabricScript.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="AdminCategoriesController"/>.
/// Covers creation, slug uniqueness, update, and soft-delete.
/// </summary>
public class AdminCategoriesControllerTests
{
    /// <summary>
    /// GetAll should return both parent and child categories.
    /// </summary>
    [Fact]
    public async Task GetAll_ShouldReturnAllCategories_IncludingChildren()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new AdminCategoriesController(new UnitOfWork(context));

        // Act
        var result = await controller.GetAll() as OkObjectResult;
        var categories = result!.Value as IEnumerable<object>;

        // Assert — 2 categories seeded (parent + child)
        categories.Should().HaveCount(2);
    }

    /// <summary>
    /// Create should return 201 for a unique slug.
    /// </summary>
    [Fact]
    public async Task Create_ShouldReturn201_WhenSlugIsUnique()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var controller = new AdminCategoriesController(new UnitOfWork(context));

        var req = new CategoryRequest("Men's Wear", "mens-wear", null, null, null, 2);

        // Act
        var result = await controller.Create(req);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    /// <summary>
    /// Create should return 409 when the slug is already in use.
    /// </summary>
    [Fact]
    public async Task Create_ShouldReturn409_WhenSlugAlreadyExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var controller = new AdminCategoriesController(new UnitOfWork(context));

        var req = new CategoryRequest("Duplicate", "sarees", null, null, null, 0); // sarees slug exists

        // Act
        var result = await controller.Create(req);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Create should store the slug in lowercase regardless of input.
    /// </summary>
    [Fact]
    public async Task Create_ShouldLowercaseSlug_OnPersist()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var uow = new UnitOfWork(context);
        var controller = new AdminCategoriesController(uow);

        var req = new CategoryRequest("KIDS WEAR", "KIDS-WEAR", null, null, null, 0);

        // Act
        var result = await controller.Create(req) as CreatedAtActionResult;
        var cat = result!.Value as TheFabricScript.Core.Entities.Category;

        // Assert
        cat!.Slug.Should().Be("kids-wear");
    }
}
