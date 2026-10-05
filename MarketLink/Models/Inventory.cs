namespace MarketLink.Models;

public class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int FarmerId { get; set; }
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public bool IsRecurringTemplate { get; set; }
    public int AvailableQuantityKg { get; set; }
    public decimal PricePerKg { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


    public Product Product { get; set; } = null!;
    public Farmer Farmer { get; set; } = null!;
}

public class PickupSlot
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public int? MarketId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxOrders { get; set; } = 20;
    public TimeOnly? CutoffTime { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }


    public Farmer Farmer { get; set; } = null!;
    public Market? Market { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
}
