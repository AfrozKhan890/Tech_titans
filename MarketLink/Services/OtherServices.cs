using MarketLink.Models;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MarketLink.Services;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;

    public CartService(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<CartItem>> GetCartAsync(int customerId)
        => await _db.CartItems
            .Include(c => c.Product).ThenInclude(p => p.Images)
            .Include(c => c.Product).ThenInclude(p => p.Farmer).ThenInclude(f => f.User)
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

    public async Task AddToCartAsync(int customerId, int productId, int quantityKg)
    {
        if (quantityKg <= 0) throw new InvalidOperationException("Quantity must be at least 1 unit.");

        var product = await _db.Products
            .Include(p => p.Farmer)
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new InvalidOperationException("That produce is no longer listed.");

        if (!product.IsAvailable || product.Farmer.Status != MarketLink.Models.Enums.FarmerStatus.Approved)
            throw new InvalidOperationException($"{product.Name} is currently unavailable.");

        if (product.StockQuantityKg < quantityKg)
            throw new InvalidOperationException($"Only {product.StockQuantityKg} {product.Unit} of {product.Name} is in stock.");
        ValidateQuantity(product, quantityKg);

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == productId);

        if (existing != null)
        {
            if (product.StockQuantityKg < existing.QuantityKg + quantityKg)
                throw new InvalidOperationException($"Only {product.StockQuantityKg} {product.Unit} of {product.Name} is in stock.");
            ValidateQuantity(product, existing.QuantityKg + quantityKg);

            existing.QuantityKg += quantityKg;
            _db.CartItems.Update(existing);
        }
        else
        {
            await _db.CartItems.AddAsync(new CartItem
            {
                CustomerId = customerId,
                ProductId = productId,
                QuantityKg = quantityKg,
                AddedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task UpdateCartItemAsync(int cartItemId, int quantityKg, int customerId)
    {
        var item = await _db.CartItems
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.CustomerId == customerId);
        if (item == null) throw new InvalidOperationException("Cart item not found.");

        if (quantityKg <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            if (item.Product.StockQuantityKg < quantityKg)
                throw new InvalidOperationException($"Only {item.Product.StockQuantityKg} {item.Product.Unit} of {item.Product.Name} is in stock.");
            item.QuantityKg = quantityKg;
            _db.CartItems.Update(item);
        }

        await _db.SaveChangesAsync();
    }

    private static void ValidateQuantity(Product product, int quantity)
    {
        if (string.IsNullOrWhiteSpace(product.AvailableQuantities)) return;
        try
        {
            var allowed = JsonSerializer.Deserialize<List<int>>(product.AvailableQuantities) ?? [];
            if (allowed.Count > 0 && !allowed.Contains(quantity))
                throw new InvalidOperationException($"{product.Name} must be ordered in one of the published quantities: {string.Join(", ", allowed)} {product.Unit}.");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"The published quantity options for {product.Name} are invalid. Please contact the farmer.");
        }
    }

    public async Task RemoveFromCartAsync(int cartItemId, int customerId)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.CustomerId == customerId);
        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task ClearCartAsync(int customerId)
    {
        var items = _db.CartItems.Where(c => c.CustomerId == customerId);
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetCartTotalAsync(int customerId)
    {
        var summary = await GetCartSummaryAsync(customerId);
        return summary.Total;
    }

    public async Task<CartSummary> GetCartSummaryAsync(int customerId)
    {
        var items = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        return new CartSummary(
            items.Count,
            items.Sum(i => i.QuantityKg),
            items.Sum(i => i.Product.PricePerKg * i.QuantityKg),
            items);
    }
}

public class FarmerService : IFarmerService
{
    private readonly ApplicationDbContext _db;

    public FarmerService(ApplicationDbContext db) => _db = db;

    public async Task<(IEnumerable<Farmer> Items, int TotalCount)> GetFarmersAsync(
        string? search, int? marketId, bool? isFeatured, int page, int pageSize)
    {
        var q = _db.Farmers
            .Include(f => f.User)
            .Include(f => f.FarmerMarkets).ThenInclude(fm => fm.Market)
            .Where(f => f.Status == MarketLink.Models.Enums.FarmerStatus.Approved);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(f => f.FarmName.Contains(search) || f.Description!.Contains(search));
        if (marketId.HasValue)
            q = q.Where(f => f.FarmerMarkets.Any(fm => fm.MarketId == marketId.Value && fm.IsActive));
        if (isFeatured.HasValue)
            q = q.Where(f => f.IsFeatured == isFeatured.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(f => f.IsFeatured)
            .ThenByDescending(f => f.RegisteredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Farmer?> GetFarmerByUserIdAsync(string userId)
        => await _db.Farmers
            .Include(f => f.User)
            .Include(f => f.FarmerMarkets).ThenInclude(fm => fm.Market)
            .Include(f => f.Products).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(f => f.UserId == userId);

    public async Task<Farmer> CreateFarmerProfileAsync(Farmer farmer)
    {
        _db.Farmers.Add(farmer);
        await _db.SaveChangesAsync();
        return farmer;
    }

    public async Task<Farmer> UpdateFarmerProfileAsync(Farmer farmer)
    {
        _db.Farmers.Update(farmer);
        await _db.SaveChangesAsync();
        return farmer;
    }

    public async Task<bool> ApproveFarmerAsync(int farmerId)
    {
        var farmer = await _db.Farmers.FindAsync(farmerId);
        if (farmer == null) return false;
        farmer.Status = MarketLink.Models.Enums.FarmerStatus.Approved;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SuspendFarmerAsync(int farmerId, string reason)
    {
        var farmer = await _db.Farmers.FindAsync(farmerId);
        if (farmer == null) return false;
        farmer.Status = MarketLink.Models.Enums.FarmerStatus.Suspended;
        farmer.AdminNotes = reason;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Farmer>> GetFeaturedFarmersAsync(int count = 6)
        => await _db.Farmers
            .Include(f => f.User)
            .Include(f => f.Products).ThenInclude(p => p.Images)
            .Where(f => f.IsFeatured && f.Status == MarketLink.Models.Enums.FarmerStatus.Approved)
            .Take(count).ToListAsync();
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task SendAsync(string userId, MarketLink.Models.Enums.NotificationType type, string title, string message, string? link = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        var q = _db.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly) q = q.Where(n => !n.IsRead);
        return await q.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var n = await _db.Notifications.FindAsync(notificationId);
        if (n != null) { n.IsRead = true; await _db.SaveChangesAsync(); }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task<int> GetUnreadCountAsync(string userId)
        => await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
}
