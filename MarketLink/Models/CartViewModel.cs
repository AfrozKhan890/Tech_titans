using MarketLink.Models;

namespace MarketLink.Models;

public class CartViewModel
{
    public List<CartFarmGroup> Groups { get; set; } = [];
    public decimal Total => Groups.Sum(g => g.Subtotal);
    public int ItemCount => Groups.Sum(g => g.Items.Count);
}

public class CartFarmGroup
{
    public int FarmerId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = [];
    public List<PickupSlot> Slots { get; set; } = [];
    public decimal Subtotal => Items.Sum(i => i.QuantityKg * i.Product.PricePerKg);
}

public class CheckoutPickup
{
    public int FarmerId { get; set; }
    public int PickupSlotId { get; set; }
    public DateTime PickupTime { get; set; }
}
