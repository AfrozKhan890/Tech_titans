using MarketLink.Models;
using MarketLink.Models.Enums;

namespace MarketLink.Services;

public interface IProductService
{
    Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? query, int? categoryId, int? farmerId, Season? season,
        bool? isOrganic, decimal? minPrice, decimal? maxPrice,
        string? sortBy, int page, int pageSize);
    Task<Product?> GetBySlugAsync(string slug);
    Task<IEnumerable<Product>> GetFeaturedAsync(int count = 8);
    Task<IEnumerable<Product>> GetBestSellersAsync(int count = 8);
    Task<IEnumerable<Product>> GetSeasonalAsync(int count = 8);
    Task<IEnumerable<Product>> GetByFarmerAsync(int farmerId);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<Product> CreateAsync(Product product, IEnumerable<string> imageUrls);
    Task<Product> UpdateAsync(Product product, IEnumerable<string>? newImageUrls = null);
    Task DeleteAsync(int productId);
}

public record PickupSelection(int FarmerId, int PickupSlotId, DateTime PickupTime);
public record OrderItemModification(int ProductId, int QuantityKg);

public interface IOrderService
{
    Task<IReadOnlyList<Order>> PlaceOrderAsync(int customerId, IEnumerable<CartItem> items, IReadOnlyCollection<PickupSelection> pickups, string? notes);
    Task<Order?> GetOrderAsync(int orderId);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetCustomerOrdersAsync(int customerId);
    Task<IEnumerable<Order>> GetFarmerOrdersAsync(int farmerId, OrderStatus? status = null);
    Task<bool> AcceptOrderAsync(int orderId, int farmerId);
    Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes = null, int? actorFarmerId = null);
    Task<bool> CancelOrderAsync(int orderId, string reason, string cancelledBy, int? actorFarmerId = null, int? actorCustomerId = null);
    Task<bool> ModifyOrderAsync(int orderId, int customerId, int pickupSlotId, DateTime pickupTime, IReadOnlyCollection<OrderItemModification> items);
}

public interface ICartService
{
    Task<IEnumerable<CartItem>> GetCartAsync(int customerId);
    Task AddToCartAsync(int customerId, int productId, int quantityKg);
    Task UpdateCartItemAsync(int cartItemId, int quantityKg, int customerId);
    Task RemoveFromCartAsync(int cartItemId, int customerId);
    Task ClearCartAsync(int customerId);
    Task<decimal> GetCartTotalAsync(int customerId);
    Task<CartSummary> GetCartSummaryAsync(int customerId);
}

public record CartSummary(int ItemCount, int TotalQuantityKg, decimal Total, IReadOnlyList<CartItem> Items);

public interface IFarmerService
{
    Task<(IEnumerable<Farmer> Items, int TotalCount)> GetFarmersAsync(
        string? search, int? marketId, bool? isFeatured, int page, int pageSize);
    Task<Farmer?> GetFarmerByUserIdAsync(string userId);
    Task<Farmer> CreateFarmerProfileAsync(Farmer farmer);
    Task<Farmer> UpdateFarmerProfileAsync(Farmer farmer);
    Task<bool> ApproveFarmerAsync(int farmerId);
    Task<bool> SuspendFarmerAsync(int farmerId, string reason);
    Task<IEnumerable<Farmer>> GetFeaturedFarmersAsync(int count = 6);
}

public interface INotificationService
{
    Task SendAsync(string userId, NotificationType type, string title, string message, string? link = null);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}
