namespace MarketLink.Models;

public class Customer
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? DefaultCity { get; set; }


    public ApplicationUser User { get; set; } = null!;
    public ICollection<CustomerAddress> Addresses { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];
}

public class CustomerAddress
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }


    public Customer Customer { get; set; } = null!;
}
