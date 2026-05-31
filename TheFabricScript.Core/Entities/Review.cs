namespace TheFabricScript.Core.Entities;

public class Review : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? OrderId { get; set; }
    public int Rating { get; set; } // 1–5
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; } = false;
    public bool IsVerifiedPurchase { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
