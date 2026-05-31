using System.Linq.Expressions;
using TheFabricScript.Core.Entities;

namespace TheFabricScript.Core.Interfaces;

/// <summary>
/// Generic repository contract that provides standard CRUD and query operations
/// for any entity that derives from <see cref="BaseEntity"/>.
///
/// <para>
/// This interface is implemented by <c>Repository&lt;T&gt;</c> in the Infrastructure layer.
/// Controllers and services depend on <see cref="IUnitOfWork"/> which exposes typed repositories
/// — they should never depend on <c>Repository&lt;T&gt;</c> directly.
/// </para>
///
/// <para><b>Soft-delete behaviour:</b> <see cref="DeleteAsync"/> sets <c>IsDeleted = true</c>
/// rather than issuing a SQL DELETE. All query methods honour the global EF Core query filter
/// that excludes soft-deleted records unless <c>IgnoreQueryFilters()</c> is called via
/// <see cref="Query"/>.</para>
/// </summary>
/// <typeparam name="T">Domain entity type that inherits from <see cref="BaseEntity"/>.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Fetches a single entity by its primary key.
    /// The global soft-delete filter is applied — deleted records return <c>null</c>.
    /// </summary>
    /// <param name="id">The entity's GUID primary key.</param>
    /// <returns>The entity, or <c>null</c> if not found or soft-deleted.</returns>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Returns all non-deleted entities of type <typeparamref name="T"/>.
    /// Avoid on large tables — prefer <see cref="Query"/> with pagination.
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Returns entities matching the given predicate.
    /// The global query filter (soft-delete) is still applied.
    /// </summary>
    /// <param name="predicate">LINQ expression used as a WHERE clause.</param>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Returns the first entity matching <paramref name="predicate"/>, or <c>null</c>.
    /// Equivalent to <c>FirstOrDefaultAsync</c> in EF Core.
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Tracks and stages the new entity for insertion.
    /// The entity is not persisted until <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// </summary>
    /// <returns>The same entity (with any DB-generated values populated after save).</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Marks the entity as modified so EF Core generates an UPDATE statement.
    /// Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Soft-deletes the entity by setting <c>IsDeleted = true</c>.
    /// The record remains in the database and is visible via admin queries with
    /// <c>IgnoreQueryFilters()</c>. No SQL DELETE is issued.
    /// </summary>
    /// <param name="id">Primary key of the entity to soft-delete.</param>
    Task DeleteAsync(Guid id);

    /// <summary>Returns <c>true</c> if at least one non-deleted entity matches <paramref name="predicate"/>.</summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Returns the count of non-deleted entities matching <paramref name="predicate"/>.
    /// If <paramref name="predicate"/> is <c>null</c>, counts all non-deleted records.
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// Exposes an <see cref="IQueryable{T}"/> for building complex queries with
    /// <c>Include</c>, <c>GroupBy</c>, pagination, etc.
    /// The global soft-delete query filter is active by default.
    /// Call <c>.IgnoreQueryFilters()</c> on the returned queryable to include deleted records.
    /// </summary>
    IQueryable<T> Query();
}
