-- =============================================================
--  THE FABRIC SCRIPT — SQL Server Database Schema
--  Database  : TheFabricScriptDb
--  Version   : 1.0  |  Date: 2026-05-31
--  Engine    : SQL Server 2019+ / Azure SQL
-- =============================================================
-- Run Order (dependencies respected):
--   1. Users, Categories
--   2. Addresses, Products
--   3. ProductVariants, ProductImages
--   4. Coupons, Carts, Wishlists
--   5. Orders, OrderItems, OrderStatusHistory, Shipments
--   6. Reviews, UserCoupons, AuditLogs
-- =============================================================

USE master;
GO

-- Create the database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TheFabricScriptDb')
BEGIN
    CREATE DATABASE TheFabricScriptDb
        COLLATE SQL_Latin1_General_CP1_CI_AS;
    PRINT 'Database TheFabricScriptDb created.';
END
GO

USE TheFabricScriptDb;
GO

-- =============================================================
-- 1. USERS
-- =============================================================
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    FirstName           NVARCHAR(100)       NOT NULL,
    LastName            NVARCHAR(100)       NOT NULL,
    Email               NVARCHAR(256)       NOT NULL,
    Phone               NVARCHAR(20)        NULL,
    PasswordHash        NVARCHAR(512)       NULL,       -- BCrypt hash; NULL for OAuth-only users
    GoogleId            NVARCHAR(256)       NULL,       -- Google OAuth subject identifier
    Role                NVARCHAR(20)        NOT NULL DEFAULT 'Customer'   -- Customer | Admin | SuperAdmin
        CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer', 'Admin', 'SuperAdmin')),
    IsEmailVerified     BIT                 NOT NULL DEFAULT 0,
    IsPhoneVerified     BIT                 NOT NULL DEFAULT 0,
    IsActive            BIT                 NOT NULL DEFAULT 1,
    ProfileImageUrl     NVARCHAR(500)       NULL,
    RefreshToken        NVARCHAR(512)       NULL,
    RefreshTokenExpiry  DATETIME2           NULL,
    OtpCode             NVARCHAR(10)        NULL,       -- Temporary; expires after 10 minutes
    OtpExpiry           DATETIME2           NULL,
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Users PRIMARY KEY (Id)
);

CREATE UNIQUE INDEX UX_Users_Email    ON dbo.Users (Email)    WHERE IsDeleted = 0;
CREATE UNIQUE INDEX UX_Users_Phone    ON dbo.Users (Phone)    WHERE Phone IS NOT NULL AND IsDeleted = 0;
CREATE UNIQUE INDEX UX_Users_GoogleId ON dbo.Users (GoogleId) WHERE GoogleId IS NOT NULL;
CREATE INDEX IX_Users_Role            ON dbo.Users (Role);
CREATE INDEX IX_Users_IsActive        ON dbo.Users (IsActive);
GO

-- =============================================================
-- 2. CATEGORIES
-- =============================================================
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
GO

CREATE TABLE dbo.Categories (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    Name        NVARCHAR(200)       NOT NULL,
    Slug        NVARCHAR(200)       NOT NULL,       -- URL-safe, e.g. "womens-wear"
    Description NVARCHAR(1000)      NULL,
    ImageUrl    NVARCHAR(500)       NULL,
    ParentId    UNIQUEIDENTIFIER    NULL,           -- NULL = top-level category
    SortOrder   INT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    IsDeleted   BIT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Categories PRIMARY KEY (Id),
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId)
        REFERENCES dbo.Categories (Id)
        ON DELETE NO ACTION
);

CREATE UNIQUE INDEX UX_Categories_Slug ON dbo.Categories (Slug) WHERE IsDeleted = 0;
CREATE INDEX IX_Categories_ParentId    ON dbo.Categories (ParentId);
CREATE INDEX IX_Categories_IsActive    ON dbo.Categories (IsActive);
GO

-- =============================================================
-- 3. PRODUCTS
-- =============================================================
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
GO

CREATE TABLE dbo.Products (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    Name                NVARCHAR(300)       NOT NULL,
    Slug                NVARCHAR(300)       NOT NULL,
    Description         NVARCHAR(MAX)       NULL,
    ShortDescription    NVARCHAR(500)       NULL,
    BasePrice           DECIMAL(18,2)       NOT NULL,
    DiscountPrice       DECIMAL(18,2)       NULL,
    Stock               INT                 NOT NULL DEFAULT 0
        CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
    SKU                 NVARCHAR(100)       NULL,
    Brand               NVARCHAR(200)       NULL,
    Material            NVARCHAR(200)       NULL,
    CareInstructions    NVARCHAR(1000)      NULL,
    IsActive            BIT                 NOT NULL DEFAULT 1,
    IsFeatured          BIT                 NOT NULL DEFAULT 0,
    ViewCount           INT                 NOT NULL DEFAULT 0,
    CategoryId          UNIQUEIDENTIFIER    NOT NULL,
    MetaTitle           NVARCHAR(300)       NULL,
    MetaDescription     NVARCHAR(500)       NULL,
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Products          PRIMARY KEY (Id),
    CONSTRAINT FK_Products_Category FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (Id)
        ON DELETE RESTRICT
);

CREATE UNIQUE INDEX UX_Products_Slug    ON dbo.Products (Slug)  WHERE IsDeleted = 0;
CREATE UNIQUE INDEX UX_Products_SKU     ON dbo.Products (SKU)   WHERE SKU IS NOT NULL AND IsDeleted = 0;
CREATE INDEX IX_Products_CategoryId     ON dbo.Products (CategoryId);
CREATE INDEX IX_Products_IsActive       ON dbo.Products (IsActive);
CREATE INDEX IX_Products_IsFeatured     ON dbo.Products (IsFeatured);
CREATE INDEX IX_Products_ViewCount      ON dbo.Products (ViewCount DESC);
CREATE INDEX IX_Products_BasePrice      ON dbo.Products (BasePrice);
GO

-- =============================================================
-- 4. PRODUCT VARIANTS
-- =============================================================
IF OBJECT_ID('dbo.ProductVariants', 'U') IS NOT NULL DROP TABLE dbo.ProductVariants;
GO

CREATE TABLE dbo.ProductVariants (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    ProductId       UNIQUEIDENTIFIER    NOT NULL,
    Size            NVARCHAR(50)        NULL,       -- XS | S | M | L | XL | XXL | Free Size
    Color           NVARCHAR(100)       NULL,
    ColorHex        NVARCHAR(10)        NULL,       -- e.g. #FF5733
    Material        NVARCHAR(200)       NULL,
    Stock           INT                 NOT NULL DEFAULT 0
        CONSTRAINT CK_Variants_Stock CHECK (Stock >= 0),
    PriceOverride   DECIMAL(18,2)       NULL,       -- NULL = use product BasePrice
    SKU             NVARCHAR(100)       NULL,
    IsActive        BIT                 NOT NULL DEFAULT 1,
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_ProductVariants          PRIMARY KEY (Id),
    CONSTRAINT FK_ProductVariants_Product  FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id)
        ON DELETE CASCADE
);

CREATE INDEX IX_ProductVariants_ProductId ON dbo.ProductVariants (ProductId);
CREATE INDEX IX_ProductVariants_IsActive  ON dbo.ProductVariants (IsActive);
GO

-- =============================================================
-- 5. PRODUCT IMAGES
-- =============================================================
IF OBJECT_ID('dbo.ProductImages', 'U') IS NOT NULL DROP TABLE dbo.ProductImages;
GO

CREATE TABLE dbo.ProductImages (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    ProductId   UNIQUEIDENTIFIER    NOT NULL,
    Url         NVARCHAR(500)       NOT NULL,
    AltText     NVARCHAR(300)       NULL,
    IsPrimary   BIT                 NOT NULL DEFAULT 0,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    MediaType   NVARCHAR(10)        NOT NULL DEFAULT 'image'  -- image | video
        CONSTRAINT CK_ProductImages_MediaType CHECK (MediaType IN ('image', 'video')),
    IsDeleted   BIT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_ProductImages         PRIMARY KEY (Id),
    CONSTRAINT FK_ProductImages_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id)
        ON DELETE CASCADE
);

CREATE INDEX IX_ProductImages_ProductId ON dbo.ProductImages (ProductId);
CREATE INDEX IX_ProductImages_IsPrimary ON dbo.ProductImages (ProductId, IsPrimary);
GO

-- =============================================================
-- 6. ADDRESSES
-- =============================================================
IF OBJECT_ID('dbo.Addresses', 'U') IS NOT NULL DROP TABLE dbo.Addresses;
GO

CREATE TABLE dbo.Addresses (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    UserId      UNIQUEIDENTIFIER    NOT NULL,
    FullName    NVARCHAR(200)       NOT NULL,
    Phone       NVARCHAR(20)        NOT NULL,
    Line1       NVARCHAR(300)       NOT NULL,
    Line2       NVARCHAR(300)       NULL,
    City        NVARCHAR(100)       NOT NULL,
    State       NVARCHAR(100)       NOT NULL,
    Pincode     NVARCHAR(10)        NOT NULL,
    Country     NVARCHAR(100)       NOT NULL DEFAULT 'India',
    Landmark    NVARCHAR(300)       NULL,
    AddressType NVARCHAR(20)        NOT NULL DEFAULT 'Home'   -- Home | Work | Other
        CONSTRAINT CK_Addresses_Type CHECK (AddressType IN ('Home', 'Work', 'Other')),
    IsDefault   BIT                 NOT NULL DEFAULT 0,
    IsDeleted   BIT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Addresses      PRIMARY KEY (Id),
    CONSTRAINT FK_Addresses_User FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id)
        ON DELETE CASCADE
);

CREATE INDEX IX_Addresses_UserId ON dbo.Addresses (UserId);
GO

-- =============================================================
-- 7. COUPONS
-- =============================================================
IF OBJECT_ID('dbo.Coupons', 'U') IS NOT NULL DROP TABLE dbo.Coupons;
GO

CREATE TABLE dbo.Coupons (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    Code                NVARCHAR(50)        NOT NULL,       -- Always stored UPPERCASE
    Description         NVARCHAR(500)       NOT NULL,
    DiscountType        NVARCHAR(20)        NOT NULL        -- Percentage | Flat
        CONSTRAINT CK_Coupons_DiscountType CHECK (DiscountType IN ('Percentage', 'Flat')),
    DiscountValue       DECIMAL(18,2)       NOT NULL
        CONSTRAINT CK_Coupons_Value CHECK (DiscountValue > 0),
    MinOrderAmount      DECIMAL(18,2)       NULL,
    MaxDiscountAmount   DECIMAL(18,2)       NULL,           -- Cap for Percentage coupons
    MaxUses             INT                 NULL,           -- NULL = unlimited
    UsedCount           INT                 NOT NULL DEFAULT 0,
    MaxUsesPerUser      INT                 NULL,
    IsActive            BIT                 NOT NULL DEFAULT 1,
    IsFirstPurchaseOnly BIT                 NOT NULL DEFAULT 0,
    ExpiresAt           DATETIME2           NULL,
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Coupons PRIMARY KEY (Id)
);

CREATE UNIQUE INDEX UX_Coupons_Code ON dbo.Coupons (Code) WHERE IsDeleted = 0;
CREATE INDEX IX_Coupons_IsActive    ON dbo.Coupons (IsActive);
GO

-- =============================================================
-- 8. ORDERS
-- =============================================================
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
GO

CREATE TABLE dbo.Orders (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    OrderNumber         NVARCHAR(30)        NOT NULL,       -- e.g. TFS-2026-000001
    UserId              UNIQUEIDENTIFIER    NOT NULL,
    ShippingAddressId   UNIQUEIDENTIFIER    NOT NULL,
    Status              NVARCHAR(30)        NOT NULL DEFAULT 'Pending'
        CONSTRAINT CK_Orders_Status CHECK (Status IN (
            'Pending','Confirmed','Packed','Shipped',
            'Delivered','Cancelled','ReturnRequested','Returned'
        )),
    Subtotal            DECIMAL(18,2)       NOT NULL,
    ShippingCharge      DECIMAL(18,2)       NOT NULL DEFAULT 0,
    TaxAmount           DECIMAL(18,2)       NOT NULL DEFAULT 0,
    DiscountAmount      DECIMAL(18,2)       NOT NULL DEFAULT 0,
    Total               DECIMAL(18,2)       NOT NULL,
    PaymentStatus       NVARCHAR(20)        NOT NULL DEFAULT 'Pending'
        CONSTRAINT CK_Orders_PaymentStatus CHECK (PaymentStatus IN (
            'Pending','Paid','Failed','Refunded'
        )),
    PaymentMethod       NVARCHAR(50)        NULL,           -- Razorpay | COD | UPI | Card | NetBanking
    RazorpayOrderId     NVARCHAR(100)       NULL,
    RazorpayPaymentId   NVARCHAR(100)       NULL,
    CouponId            UNIQUEIDENTIFIER    NULL,
    Notes               NVARCHAR(1000)      NULL,
    DeliveredAt         DATETIME2           NULL,
    CancelledAt         DATETIME2           NULL,
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Orders                    PRIMARY KEY (Id),
    CONSTRAINT FK_Orders_User               FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Orders_Address            FOREIGN KEY (ShippingAddressId)
        REFERENCES dbo.Addresses (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Orders_Coupon             FOREIGN KEY (CouponId)
        REFERENCES dbo.Coupons (Id) ON DELETE SET NULL
);

CREATE UNIQUE INDEX UX_Orders_OrderNumber   ON dbo.Orders (OrderNumber);
CREATE INDEX IX_Orders_UserId               ON dbo.Orders (UserId);
CREATE INDEX IX_Orders_Status               ON dbo.Orders (Status);
CREATE INDEX IX_Orders_PaymentStatus        ON dbo.Orders (PaymentStatus);
CREATE INDEX IX_Orders_CreatedAt            ON dbo.Orders (CreatedAt DESC);
GO

-- =============================================================
-- 9. ORDER ITEMS
-- =============================================================
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
GO

CREATE TABLE dbo.OrderItems (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    OrderId             UNIQUEIDENTIFIER    NOT NULL,
    ProductId           UNIQUEIDENTIFIER    NOT NULL,
    VariantId           UNIQUEIDENTIFIER    NULL,
    ProductName         NVARCHAR(300)       NOT NULL,       -- Snapshotted at order time
    VariantDescription  NVARCHAR(200)       NULL,           -- e.g. "Blue / M"
    ProductImageUrl     NVARCHAR(500)       NULL,
    Quantity            INT                 NOT NULL
        CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    UnitPrice           DECIMAL(18,2)       NOT NULL,
    TotalPrice          DECIMAL(18,2)       NOT NULL,
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_OrderItems            PRIMARY KEY (Id),
    CONSTRAINT FK_OrderItems_Order      FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Product    FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_OrderItems_Variant    FOREIGN KEY (VariantId)
        REFERENCES dbo.ProductVariants (Id) ON DELETE NO ACTION
);

CREATE INDEX IX_OrderItems_OrderId   ON dbo.OrderItems (OrderId);
CREATE INDEX IX_OrderItems_ProductId ON dbo.OrderItems (ProductId);
GO

-- =============================================================
-- 10. ORDER STATUS HISTORY
-- =============================================================
IF OBJECT_ID('dbo.OrderStatusHistories', 'U') IS NOT NULL DROP TABLE dbo.OrderStatusHistories;
GO

CREATE TABLE dbo.OrderStatusHistories (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    OrderId             UNIQUEIDENTIFIER    NOT NULL,
    Status              NVARCHAR(30)        NOT NULL,
    Comment             NVARCHAR(500)       NULL,
    ChangedByUserId     UNIQUEIDENTIFIER    NULL,           -- NULL = system-generated
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_OrderStatusHistories      PRIMARY KEY (Id),
    CONSTRAINT FK_StatusHistory_Order       FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders (Id) ON DELETE CASCADE
);

CREATE INDEX IX_OrderStatusHistories_OrderId ON dbo.OrderStatusHistories (OrderId);
GO

-- =============================================================
-- 11. SHIPMENTS
-- =============================================================
IF OBJECT_ID('dbo.Shipments', 'U') IS NOT NULL DROP TABLE dbo.Shipments;
GO

CREATE TABLE dbo.Shipments (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    OrderId             UNIQUEIDENTIFIER    NOT NULL,
    AwbNumber           NVARCHAR(100)       NULL,           -- Airway Bill Number from courier
    CourierName         NVARCHAR(100)       NULL,           -- Delhivery | Shiprocket | BlueDart
    TrackingUrl         NVARCHAR(500)       NULL,
    ShiprocketOrderId   NVARCHAR(100)       NULL,
    Status              NVARCHAR(50)        NOT NULL DEFAULT 'Pending',
    EstimatedDelivery   DATETIME2           NULL,
    ShippedAt           DATETIME2           NULL,
    DeliveredAt         DATETIME2           NULL,
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Shipments         PRIMARY KEY (Id),
    CONSTRAINT FK_Shipments_Order   FOREIGN KEY (OrderId)
        REFERENCES dbo.Orders (Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX UX_Shipments_OrderId ON dbo.Shipments (OrderId) WHERE IsDeleted = 0;
GO

-- =============================================================
-- 12. CART ITEMS
-- =============================================================
IF OBJECT_ID('dbo.CartItems', 'U') IS NOT NULL DROP TABLE dbo.CartItems;
GO

CREATE TABLE dbo.CartItems (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    UserId          UNIQUEIDENTIFIER    NULL,               -- NULL = guest cart (use SessionId)
    SessionId       NVARCHAR(100)       NULL,               -- Guest session token
    ProductId       UNIQUEIDENTIFIER    NOT NULL,
    VariantId       UNIQUEIDENTIFIER    NULL,
    Quantity        INT                 NOT NULL DEFAULT 1
        CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0),
    SavedForLater   BIT                 NOT NULL DEFAULT 0,
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_CartItems             PRIMARY KEY (Id),
    CONSTRAINT FK_CartItems_User        FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Product     FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_CartItems_Variant     FOREIGN KEY (VariantId)
        REFERENCES dbo.ProductVariants (Id) ON DELETE NO ACTION
);

CREATE INDEX IX_CartItems_UserId    ON dbo.CartItems (UserId)    WHERE UserId IS NOT NULL;
CREATE INDEX IX_CartItems_SessionId ON dbo.CartItems (SessionId) WHERE SessionId IS NOT NULL;
GO

-- =============================================================
-- 13. WISHLIST ITEMS
-- =============================================================
IF OBJECT_ID('dbo.WishlistItems', 'U') IS NOT NULL DROP TABLE dbo.WishlistItems;
GO

CREATE TABLE dbo.WishlistItems (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    UserId      UNIQUEIDENTIFIER    NOT NULL,
    ProductId   UNIQUEIDENTIFIER    NOT NULL,
    IsDeleted   BIT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_WishlistItems         PRIMARY KEY (Id),
    CONSTRAINT FK_WishlistItems_User    FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_WishlistItems_Product FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX UX_WishlistItems_UserProduct
    ON dbo.WishlistItems (UserId, ProductId) WHERE IsDeleted = 0;
GO

-- =============================================================
-- 14. REVIEWS
-- =============================================================
IF OBJECT_ID('dbo.Reviews', 'U') IS NOT NULL DROP TABLE dbo.Reviews;
GO

CREATE TABLE dbo.Reviews (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    UserId              UNIQUEIDENTIFIER    NOT NULL,
    ProductId           UNIQUEIDENTIFIER    NOT NULL,
    OrderId             UNIQUEIDENTIFIER    NULL,
    Rating              INT                 NOT NULL
        CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5),
    Title               NVARCHAR(200)       NULL,
    Comment             NVARCHAR(2000)      NULL,
    IsApproved          BIT                 NOT NULL DEFAULT 0,
    IsVerifiedPurchase  BIT                 NOT NULL DEFAULT 0,
    IsDeleted           BIT                 NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Reviews           PRIMARY KEY (Id),
    CONSTRAINT FK_Reviews_User      FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Reviews_Product   FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id) ON DELETE NO ACTION
);

CREATE INDEX IX_Reviews_ProductId   ON dbo.Reviews (ProductId);
CREATE INDEX IX_Reviews_IsApproved  ON dbo.Reviews (IsApproved);
-- One review per user per product (enforce in application layer, index for perf)
CREATE INDEX IX_Reviews_UserProduct ON dbo.Reviews (UserId, ProductId);
GO

-- =============================================================
-- 15. USER COUPONS (usage tracking)
-- =============================================================
IF OBJECT_ID('dbo.UserCoupons', 'U') IS NOT NULL DROP TABLE dbo.UserCoupons;
GO

CREATE TABLE dbo.UserCoupons (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    UserId      UNIQUEIDENTIFIER    NOT NULL,
    CouponId    UNIQUEIDENTIFIER    NOT NULL,
    UsedCount   INT                 NOT NULL DEFAULT 0,
    IsDeleted   BIT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_UserCoupons           PRIMARY KEY (Id),
    CONSTRAINT FK_UserCoupons_User      FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserCoupons_Coupon    FOREIGN KEY (CouponId)
        REFERENCES dbo.Coupons (Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX UX_UserCoupons_UserCoupon
    ON dbo.UserCoupons (UserId, CouponId) WHERE IsDeleted = 0;
GO

-- =============================================================
-- 16. AUDIT LOGS (immutable — never soft-deleted)
-- =============================================================
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
GO

CREATE TABLE dbo.AuditLogs (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    UserId      UNIQUEIDENTIFIER    NULL,           -- NULL = anonymous / system
    Action      NVARCHAR(100)       NOT NULL,       -- e.g. LOGIN | PRODUCT_UPDATED | ORDER_STATUS_CHANGED
    EntityName  NVARCHAR(100)       NOT NULL,       -- e.g. Product | Order | User
    EntityId    NVARCHAR(100)       NULL,
    OldValues   NVARCHAR(MAX)       NULL,           -- JSON snapshot before change
    NewValues   NVARCHAR(MAX)       NULL,           -- JSON snapshot after change
    IpAddress   NVARCHAR(45)        NULL,           -- IPv4 or IPv6
    UserAgent   NVARCHAR(500)       NULL,
    IsDeleted   BIT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AuditLogs PRIMARY KEY (Id)
);

CREATE INDEX IX_AuditLogs_UserId     ON dbo.AuditLogs (UserId);
CREATE INDEX IX_AuditLogs_EntityName ON dbo.AuditLogs (EntityName, EntityId);
CREATE INDEX IX_AuditLogs_CreatedAt  ON dbo.AuditLogs (CreatedAt DESC);
GO

-- =============================================================
-- SEED DATA — Default SuperAdmin
-- =============================================================
-- Password: Admin@Fabric2026  (BCrypt hash below — change in production!)
INSERT INTO dbo.Users (Id, FirstName, LastName, Email, Role, IsEmailVerified, IsActive, PasswordHash)
VALUES (
    NEWID(),
    'Super', 'Admin',
    'admin@thefabricscript.com',
    'SuperAdmin',
    1, 1,
    '$2a$11$examplehashchangethisbeforegoingproduction'
);
GO

PRINT '============================================';
PRINT ' TheFabricScript schema created successfully';
PRINT '============================================';
