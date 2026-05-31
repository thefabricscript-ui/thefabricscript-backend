namespace TheFabricScript.Core.Entities;

/// <summary>
/// Abstract base class for all domain entities.
/// Provides common audit fields and soft-delete support.
/// Every table in the database has these columns.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Primary key. Uses a client-generated GUID (Guid.NewGuid()) so IDs
    /// are safe to create in application code before they are persisted.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>UTC timestamp when the record was first inserted.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the last update. Automatically refreshed by
    /// <see cref="TheFabricScript.Infrastructure.Data.AppDbContext.SaveChangesAsync"/>.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft-delete flag. When <c>true</c> the record is excluded from all
    /// standard queries via EF Core global query filters.
    /// Use <c>IgnoreQueryFilters()</c> in admin queries to see deleted records.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
