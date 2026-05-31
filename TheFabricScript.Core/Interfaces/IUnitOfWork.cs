using TheFabricScript.Core.Entities;

namespace TheFabricScript.Core.Interfaces;

/// <summary>
/// Unit of Work pattern. Groups all repositories under a single database context
/// so multiple repository operations can be committed atomically in one
/// <see cref="SaveChangesAsync"/> call.
///
/// <para><b>Usage pattern:</b></para>
/// <code>
/// // In a controller or service:
/// await _uow.Products.AddAsync(product);
/// await _uow.AuditLogs.AddAsync(log);
/// await _uow.SaveChangesAsync(); // both inserts committed in one transaction
/// </code>
///
/// <para>Register as <c>Scoped</c> in DI — one instance per HTTP request.</para>
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Repository for <see cref="User"/> entities.</summary>
    IRepository<User> Users { get; }

    /// <summary>Repository for <see cref="Product"/> entities.</summary>
    IRepository<Product> Products { get; }

    /// <summary>Repository for <see cref="ProductVariant"/> entities (size/colour variants).</summary>
    IRepository<ProductVariant> ProductVariants { get; }

    /// <summary>Repository for <see cref="ProductImage"/> entities.</summary>
    IRepository<ProductImage> ProductImages { get; }

    /// <summary>Repository for <see cref="Category"/> entities (supports parent/child hierarchy).</summary>
    IRepository<Category> Categories { get; }

    /// <summary>Repository for <see cref="Order"/> entities.</summary>
    IRepository<Order> Orders { get; }

    /// <summary>Repository for <see cref="OrderItem"/> line items within orders.</summary>
    IRepository<OrderItem> OrderItems { get; }

    /// <summary>Repository for <see cref="OrderStatusHistory"/> audit trail of status changes.</summary>
    IRepository<OrderStatusHistory> OrderStatusHistories { get; }

    /// <summary>Repository for <see cref="Address"/> entities (user delivery addresses).</summary>
    IRepository<Address> Addresses { get; }

    /// <summary>Repository for <see cref="CartItem"/> entities (persistent cart, supports guest sessions).</summary>
    IRepository<CartItem> CartItems { get; }

    /// <summary>Repository for <see cref="WishlistItem"/> entities.</summary>
    IRepository<WishlistItem> WishlistItems { get; }

    /// <summary>Repository for <see cref="Review"/> entities (requires admin approval before display).</summary>
    IRepository<Review> Reviews { get; }

    /// <summary>Repository for <see cref="Coupon"/> discount codes.</summary>
    IRepository<Coupon> Coupons { get; }

    /// <summary>Repository for <see cref="UserCoupon"/> — tracks per-user coupon usage counts.</summary>
    IRepository<UserCoupon> UserCoupons { get; }

    /// <summary>Repository for <see cref="Shipment"/> tracking records linked to orders.</summary>
    IRepository<Shipment> Shipments { get; }

    /// <summary>Repository for <see cref="AuditLog"/> immutable audit trail records.</summary>
    IRepository<AuditLog> AuditLogs { get; }

    /// <summary>
    /// Persists all pending changes tracked by the underlying <c>DbContext</c> in a single transaction.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync();
}
