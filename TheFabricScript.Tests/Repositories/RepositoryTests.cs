using FluentAssertions;
using TheFabricScript.Core.Entities;
using TheFabricScript.Infrastructure.Repositories;
using TheFabricScript.Tests.Helpers;
using Xunit;

namespace TheFabricScript.Tests.Repositories;

/// <summary>
/// Unit tests for the generic <see cref="Repository{T}"/> implementation.
/// Uses EF Core InMemory database — each test method gets its own isolated database.
/// </summary>
public class RepositoryTests
{
    // ── AddAsync ─────────────────────────────────────────

    /// <summary>
    /// Given a new entity, AddAsync should stage it for insert.
    /// After SaveChangesAsync the entity should be retrievable by its ID.
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldPersistEntity_WhenSaved()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var repo = new Repository<Category>(context);
        var category = new Category { Name = "Test", Slug = "test", IsActive = true };

        // Act
        await repo.AddAsync(category);
        await context.SaveChangesAsync();

        // Assert
        var result = await repo.GetByIdAsync(category.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    // ── GetByIdAsync ──────────────────────────────────────

    /// <summary>
    /// GetByIdAsync should return the correct entity when a matching ID exists.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);

        // Act
        var result = await repo.GetByIdAsync(SeedData.ProductId1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(SeedData.ProductId1);
        result.Name.Should().Be("Floral Cotton Kurta");
    }

    /// <summary>
    /// GetByIdAsync should return null when the ID does not exist.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var repo = new Repository<Product>(context);

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// GetByIdAsync should return null for soft-deleted records
    /// (the global query filter should exclude them).
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenSoftDeleted()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);

        // Soft delete the product
        await repo.DeleteAsync(SeedData.ProductId1);
        await context.SaveChangesAsync();

        // Act — standard query (global filter active)
        var result = await repo.GetByIdAsync(SeedData.ProductId1);

        // Assert
        result.Should().BeNull("soft-deleted records must be excluded by default");
    }

    // ── UpdateAsync ───────────────────────────────────────

    /// <summary>
    /// UpdateAsync should mark the entity as modified so EF Core
    /// generates the correct UPDATE statement on SaveChangesAsync.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges_WhenSaved()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);

        var product = await repo.GetByIdAsync(SeedData.ProductId1);
        product!.Name = "Updated Kurta Name";

        // Act
        await repo.UpdateAsync(product);
        await context.SaveChangesAsync();

        // Assert
        var updated = await repo.GetByIdAsync(SeedData.ProductId1);
        updated!.Name.Should().Be("Updated Kurta Name");
    }

    // ── DeleteAsync (soft delete) ─────────────────────────

    /// <summary>
    /// DeleteAsync should set IsDeleted = true without physically removing the row.
    /// The record should still exist in the database but be invisible to standard queries.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_NotHardDelete()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);

        // Act
        await repo.DeleteAsync(SeedData.ProductId1);
        await context.SaveChangesAsync();

        // Assert — not visible via standard query
        var afterDelete = await repo.GetByIdAsync(SeedData.ProductId1);
        afterDelete.Should().BeNull();

        // Assert — still physically in the database
        var rawRecord = context.Products
            .IgnoreQueryFilters()
            .FirstOrDefault(p => p.Id == SeedData.ProductId1);
        rawRecord.Should().NotBeNull();
        rawRecord!.IsDeleted.Should().BeTrue();
    }

    // ── ExistsAsync ───────────────────────────────────────

    /// <summary>
    /// ExistsAsync should return true when a matching non-deleted entity exists.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenEntityExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);

        // Act
        var exists = await repo.ExistsAsync(p => p.Slug == "floral-cotton-kurta");

        // Assert
        exists.Should().BeTrue();
    }

    /// <summary>
    /// ExistsAsync should return false for soft-deleted records.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenSoftDeleted()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);
        await repo.DeleteAsync(SeedData.ProductId1);
        await context.SaveChangesAsync();

        // Act
        var exists = await repo.ExistsAsync(p => p.Id == SeedData.ProductId1);

        // Assert
        exists.Should().BeFalse();
    }

    // ── CountAsync ────────────────────────────────────────

    /// <summary>
    /// CountAsync with no predicate should return the total count of non-deleted records.
    /// </summary>
    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount_WithNoPredicate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);

        // Act
        var count = await repo.CountAsync();

        // Assert
        count.Should().Be(2); // Product1 and Product2
    }

    /// <summary>
    /// CountAsync with a predicate should count only matching records.
    /// </summary>
    [Fact]
    public async Task CountAsync_ShouldReturnFilteredCount_WithPredicate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);

        // Act — only active products with stock > 0
        var count = await repo.CountAsync(p => p.Stock > 0 && p.IsActive);

        // Assert
        count.Should().Be(1); // Only Product1 has stock
    }

    // ── FindAsync ─────────────────────────────────────────

    /// <summary>
    /// FindAsync should return all entities matching the predicate.
    /// </summary>
    [Fact]
    public async Task FindAsync_ShouldReturnMatchingEntities()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        await SeedData.SeedBasicAsync(context);
        var repo = new Repository<Product>(context);

        // Act
        var results = (await repo.FindAsync(p => p.Brand == "The Fabric Script")).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(p => p.Brand.Should().Be("The Fabric Script"));
    }
}
