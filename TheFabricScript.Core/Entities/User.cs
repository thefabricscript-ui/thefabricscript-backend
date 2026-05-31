namespace TheFabricScript.Core.Entities;

/// <summary>
/// Represents a platform user — customer, admin, or super-admin.
/// Supports three authentication methods: email+password, Google OAuth, and phone OTP.
/// </summary>
public class User : BaseEntity
{
    /// <summary>User's first name. Required.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>User's last name. Required.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Unique email address. Used as the primary login identifier for
    /// email/password and Google OAuth flows.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mobile phone number in E.164 format (e.g. +919876543210).
    /// Required for OTP login; optional for email/Google sign-up.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// BCrypt-hashed password. Null for users who registered via Google OAuth or OTP only.
    /// Never expose this field in API responses.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Google OAuth subject identifier. Populated on first Google login.
    /// Used to look up returning Google users without checking email alone.
    /// </summary>
    public string? GoogleId { get; set; }

    /// <summary>
    /// RBAC role. One of: <c>Customer</c>, <c>Admin</c>, <c>SuperAdmin</c>.
    /// Controls access to admin panel routes and destructive operations.
    /// </summary>
    public string Role { get; set; } = "Customer";

    /// <summary>True once the user clicks the verification link sent to their email.</summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>True once the user successfully completes OTP verification for their phone.</summary>
    public bool IsPhoneVerified { get; set; } = false;

    /// <summary>
    /// Account status flag. Admins can deactivate accounts (sets this to false).
    /// Deactivated users receive a 401 on login attempts.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>URL to the user's profile image. Typically sourced from Google or uploaded by the user.</summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// Opaque refresh token stored as a hashed string.
    /// Used to issue new access tokens without re-authentication.
    /// Invalidated on logout or when a new refresh is issued.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>UTC expiry of the current refresh token. Null means no active token.</summary>
    public DateTime? RefreshTokenExpiry { get; set; }

    /// <summary>
    /// One-time password sent to the user's phone via SMS.
    /// Stored in plain text temporarily; expires after <see cref="OtpExpiry"/>.
    /// </summary>
    public string? OtpCode { get; set; }

    /// <summary>UTC expiry of the current OTP. OTPs are valid for 10 minutes.</summary>
    public DateTime? OtpExpiry { get; set; }

    // ── Navigation Properties ─────────────────────────────

    /// <summary>All orders placed by this user.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>Saved delivery addresses.</summary>
    public ICollection<Address> Addresses { get; set; } = new List<Address>();

    /// <summary>Products the user has wishlisted.</summary>
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

    /// <summary>Product reviews submitted by this user.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Tracks which coupons this user has used and how many times.</summary>
    public ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
}
