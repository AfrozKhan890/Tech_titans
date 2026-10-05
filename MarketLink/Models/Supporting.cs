using MarketLink.Models.Enums;

namespace MarketLink.Models;

public class Review
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? ProductId { get; set; }
    public int? FarmerId { get; set; }
    public int? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public string? FarmerResponse { get; set; }
    public DateTime? FarmerRespondedAt { get; set; }
    public bool IsApproved { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public Customer Customer { get; set; } = null!;
    public Product? Product { get; set; }
    public Farmer? Farmer { get; set; }
    public Order? Order { get; set; }
}

public class Favorite
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? ProductId { get; set; }
    public int? FarmerId { get; set; }
    public int? MarketId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public Customer Customer { get; set; } = null!;
    public Product? Product { get; set; }
    public Farmer? Farmer { get; set; }
    public Market? Market { get; set; }
}

public class CartItem
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int QuantityKg { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;


    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public ApplicationUser User { get; set; } = null!;
}

public class Newsletter
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnsubscribedAt { get; set; }
}

public class ContactMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? AdminReply { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class SiteSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = "General";
}

public class Report
{
    public int Id { get; set; }
    public ReportType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? GeneratedBy { get; set; }
    public string? FilePath { get; set; }
    public string? Parameters { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
