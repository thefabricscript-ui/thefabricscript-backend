using TheFabricScript.Core.Entities;
using TheFabricScript.Core.Interfaces;
using TheFabricScript.Infrastructure.Data;

namespace TheFabricScript.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        Products = new Repository<Product>(context);
        ProductVariants = new Repository<ProductVariant>(context);
        ProductImages = new Repository<ProductImage>(context);
        Categories = new Repository<Category>(context);
        Orders = new Repository<Order>(context);
        OrderItems = new Repository<OrderItem>(context);
        OrderStatusHistories = new Repository<OrderStatusHistory>(context);
        Addresses = new Repository<Address>(context);
        CartItems = new Repository<CartItem>(context);
        WishlistItems = new Repository<WishlistItem>(context);
        Reviews = new Repository<Review>(context);
        Coupons = new Repository<Coupon>(context);
        UserCoupons = new Repository<UserCoupon>(context);
        Shipments = new Repository<Shipment>(context);
        AuditLogs = new Repository<AuditLog>(context);
    }

    public IRepository<User> Users { get; }
    public IRepository<Product> Products { get; }
    public IRepository<ProductVariant> ProductVariants { get; }
    public IRepository<ProductImage> ProductImages { get; }
    public IRepository<Category> Categories { get; }
    public IRepository<Order> Orders { get; }
    public IRepository<OrderItem> OrderItems { get; }
    public IRepository<OrderStatusHistory> OrderStatusHistories { get; }
    public IRepository<Address> Addresses { get; }
    public IRepository<CartItem> CartItems { get; }
    public IRepository<WishlistItem> WishlistItems { get; }
    public IRepository<Review> Reviews { get; }
    public IRepository<Coupon> Coupons { get; }
    public IRepository<UserCoupon> UserCoupons { get; }
    public IRepository<Shipment> Shipments { get; }
    public IRepository<AuditLog> AuditLogs { get; }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
