using System.Data;
using System.Text.Json;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public OrderService(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<Order>> PlaceOrderAsync(
        int customerId,
        IEnumerable<CartItem> items,
        IReadOnlyCollection<PickupSelection> pickups,
        string? notes)
    {
        var cartList = items.ToList();
        if (cartList.Count == 0) throw new InvalidOperationException("Cart is empty.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId)
            ?? throw new InvalidOperationException("Customer profile not found.");

        var productIds = cartList.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.Farmer)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more products are no longer available.");

        var selectionByFarmer = (pickups ?? Array.Empty<PickupSelection>())
            .GroupBy(p => p.FarmerId)
            .ToDictionary(g => g.Key, g => g.First());

        var farmerIds = products.Values.Select(p => p.FarmerId).Distinct().ToList();
        var slots = await _db.PickupSlots
            .Include(s => s.Market)
            .Where(s => farmerIds.Contains(s.FarmerId) && s.IsActive)
            .ToListAsync();

        var orders = new List<Order>();
        var farmerUserIds = new Dictionary<Order, string>();

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            foreach (var group in cartList.GroupBy(i => products[i.ProductId].FarmerId))
            {
                var farmerId = group.Key;
                var farmer = products[group.First().ProductId].Farmer;
                if (farmer.Status != FarmerStatus.Approved)
                    throw new InvalidOperationException($"{farmer.FarmName} is not accepting orders yet.");

                if (!selectionByFarmer.TryGetValue(farmerId, out var selection))
                    throw new InvalidOperationException("Please choose a pickup slot for every farm in your basket.");

                var slot = slots.FirstOrDefault(s => s.Id == selection.PickupSlotId && s.FarmerId == farmerId)
                    ?? throw new InvalidOperationException("The selected pickup slot is no longer available for this farm.");

                ValidatePickupSelection(slot, selection.PickupTime);
                await EnsureSlotCapacityAsync(slot.Id, slot.MaxOrders);

                var order = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    CustomerId = customerId,
                    FarmerId = farmerId,
                    PickupSlotId = slot.Id,
                    MarketId = slot.MarketId,
                    PickupTime = NormalizePickupTime(selection.PickupTime),
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    Status = OrderStatus.Pending,
                    OrderedAt = DateTime.UtcNow
                };

                decimal subtotal = 0;
                foreach (var cartItem in group)
                {
                    if (cartItem.QuantityKg <= 0)
                        throw new InvalidOperationException($"Quantity for product {cartItem.ProductId} must be greater than zero.");

                    var product = products[cartItem.ProductId];
                    if (product.FarmerId != farmerId)
                        throw new InvalidOperationException("A basket item does not belong to its selected farmer.");
                    if (!product.IsAvailable)
                        throw new InvalidOperationException($"{product.Name} is no longer available.");
                    ValidateProductQuantity(product, cartItem.QuantityKg);



                    var affected = await _db.Products
                        .Where(p => p.Id == product.Id && p.StockQuantityKg >= cartItem.QuantityKg && p.IsAvailable)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.StockQuantityKg, p => p.StockQuantityKg - cartItem.QuantityKg)
                            .SetProperty(p => p.UpdatedAt, _ => DateTime.UtcNow));

                    if (affected != 1)
                        throw new InvalidOperationException($"Only the currently available stock of {product.Name} can be ordered.");

                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        QuantityKg = cartItem.QuantityKg,
                        PricePerKgSnapshot = product.PricePerKg,
                        TotalPrice = product.PricePerKg * cartItem.QuantityKg,
                        ProductNameSnapshot = product.Name,
                        UnitSnapshot = string.IsNullOrWhiteSpace(product.Unit) ? "KG" : product.Unit.Trim()
                    };
                    order.Items.Add(orderItem);
                    subtotal += orderItem.TotalPrice;
                }

                order.SubTotal = subtotal;
                order.TotalAmount = subtotal;
                orders.Add(order);
                farmerUserIds[order] = farmer.UserId;
                _db.Orders.Add(order);
            }

            var trackedRows = await _db.CartItems
                .Where(c => c.CustomerId == customerId && productIds.Contains(c.ProductId))
                .ToListAsync();
            var staleRows = trackedRows
                .Where(r => cartList.Any(i => i.Id > 0 ? i.Id == r.Id : i.ProductId == r.ProductId))
                .ToList();
            _db.CartItems.RemoveRange(staleRows);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        foreach (var order in orders)
        {
            await _notificationService.SendAsync(
                customer.UserId, NotificationType.OrderPlaced,
                "Order placed", $"Your order #{order.OrderNumber} was placed successfully.",
                $"/Customer/Orders/Details/{order.Id}");

            await _notificationService.SendAsync(
                farmerUserIds[order], NotificationType.OrderPlaced,
                "New order received", $"Order #{order.OrderNumber} is waiting for your response.",
                $"/Farmer/Orders/Details/{order.Id}");
        }

        return orders;
    }

    public async Task<Order?> GetOrderAsync(int orderId)
        => await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Farmer).ThenInclude(f => f.User)
            .Include(o => o.Market)
            .Include(o => o.PickupSlot)
            .FirstOrDefaultAsync(o => o.Id == orderId);

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        => await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Farmer).ThenInclude(f => f.User)
            .Include(o => o.PickupSlot)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task<IEnumerable<Order>> GetCustomerOrdersAsync(int customerId)
        => await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .Include(o => o.Farmer).ThenInclude(f => f.User)
            .Include(o => o.PickupSlot)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync();

    public async Task<IEnumerable<Order>> GetFarmerOrdersAsync(int farmerId, OrderStatus? status = null)
    {
        var q = _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.PickupSlot)
            .Where(o => o.FarmerId == farmerId);

        if (status.HasValue) q = q.Where(o => o.Status == status.Value);
        return await q.OrderByDescending(o => o.OrderedAt).ToListAsync();
    }

    public async Task<bool> AcceptOrderAsync(int orderId, int farmerId)
        => await UpdateOrderStatusAsync(orderId, OrderStatus.Accepted, null, farmerId);

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes = null, int? actorFarmerId = null)
    {
        var order = await _db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;
        if (actorFarmerId.HasValue && order.FarmerId != actorFarmerId.Value) return false;

        if (!IsValidTransition(order.Status, newStatus)) return false;

        order.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(notes)) order.FarmerNotes = notes.Trim();
        if (newStatus == OrderStatus.Accepted) order.AcceptedAt ??= DateTime.UtcNow;
        if (newStatus == OrderStatus.ReadyForPickup) order.ReadyAt = DateTime.UtcNow;
        if (newStatus == OrderStatus.Completed) order.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        if (newStatus == OrderStatus.ReadyForPickup)
            await _notificationService.SendAsync(order.Customer.UserId, NotificationType.OrderReady,
                "Order Ready!", $"Your order #{order.OrderNumber} is ready for pickup!",
                $"/Customer/Orders/Details/{order.Id}");
        else if (newStatus == OrderStatus.Completed)
            await _notificationService.SendAsync(order.Customer.UserId, NotificationType.OrderCompleted,
                "Order completed", $"Thank you for collecting order #{order.OrderNumber}.",
                $"/Customer/Orders/Details/{order.Id}");
        else if (newStatus == OrderStatus.Accepted)
            await _notificationService.SendAsync(order.Customer.UserId, NotificationType.OrderAccepted,
                "Order Accepted", $"Your order #{order.OrderNumber} has been accepted by the farmer.",
                $"/Customer/Orders/Details/{order.Id}");

        return true;
    }

    public async Task<bool> CancelOrderAsync(int orderId, string reason, string cancelledBy, int? actorFarmerId = null, int? actorCustomerId = null)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Farmer).ThenInclude(f => f.User)
            .Include(o => o.PickupSlot)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;

        if (actorFarmerId.HasValue && order.FarmerId != actorFarmerId.Value) return false;
        if (actorCustomerId.HasValue && order.CustomerId != actorCustomerId.Value) return false;
        if (!CanCancel(order, cancelledBy)) return false;

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Order cancelled." : reason.Trim();
            order.CancelledAt = DateTime.UtcNow;

            foreach (var item in order.Items)
            {
                await _db.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.StockQuantityKg, p => p.StockQuantityKg + item.QuantityKg)
                        .SetProperty(p => p.UpdatedAt, _ => DateTime.UtcNow));
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var message = $"Order #{order.OrderNumber} has been cancelled. Reason: {order.CancellationReason}";
        if (cancelledBy.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            await _notificationService.SendAsync(order.Farmer.UserId, NotificationType.OrderCancelled,
                "Order cancelled by customer", message, $"/Farmer/Orders/Details/{order.Id}");
        }
        else
        {
            await _notificationService.SendAsync(order.Customer.UserId, NotificationType.OrderCancelled,
                "Order Cancelled", message, $"/Customer/Orders/Details/{order.Id}");
        }

        await NotifyRestockedFavoritesAsync(order.Items);
        return true;
    }

    public async Task<bool> ModifyOrderAsync(int orderId, int customerId, int pickupSlotId, DateTime pickupTime, IReadOnlyCollection<OrderItemModification> items)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.PickupSlot)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);
        if (order == null || !CanModify(order)) return false;

        if (items.Count == 0) throw new InvalidOperationException("At least one order item is required.");
        if (items.Any(i => i.QuantityKg <= 0)) throw new InvalidOperationException("Order quantities must be greater than zero.");
        if (items.Select(i => i.ProductId).Distinct().Count() != items.Count) throw new InvalidOperationException("A product can appear only once in an order.");

        var slot = await _db.PickupSlots
            .FirstOrDefaultAsync(s => s.Id == pickupSlotId && s.FarmerId == order.FarmerId && s.IsActive);
        if (slot == null) throw new InvalidOperationException("The selected pickup slot is not available for this farmer.");
        ValidatePickupSelection(slot, pickupTime);

        if (slot.Id != order.PickupSlotId)
            await EnsureSlotCapacityAsync(slot.Id, slot.MaxOrders);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var requestedIds = items.Select(i => i.ProductId).ToList();
            var products = await _db.Products
                .Where(p => requestedIds.Contains(p.Id) && p.FarmerId == order.FarmerId)
                .ToDictionaryAsync(p => p.Id);
            if (products.Count != requestedIds.Count)
                throw new InvalidOperationException("One or more selected products are not owned by this farmer.");


            foreach (var oldItem in order.Items)
            {
                await _db.Products
                    .Where(p => p.Id == oldItem.ProductId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.StockQuantityKg, p => p.StockQuantityKg + oldItem.QuantityKg)
                        .SetProperty(p => p.UpdatedAt, _ => DateTime.UtcNow));
            }

            foreach (var requested in items)
            {
                var product = products[requested.ProductId];
                if (!product.IsAvailable) throw new InvalidOperationException($"{product.Name} is no longer available.");
                ValidateProductQuantity(product, requested.QuantityKg);

                var affected = await _db.Products
                    .Where(p => p.Id == requested.ProductId && p.StockQuantityKg >= requested.QuantityKg && p.IsAvailable)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.StockQuantityKg, p => p.StockQuantityKg - requested.QuantityKg)
                        .SetProperty(p => p.UpdatedAt, _ => DateTime.UtcNow));
                if (affected != 1)
                    throw new InvalidOperationException($"Not enough stock is available for {product.Name}.");
            }

            _db.OrderItems.RemoveRange(order.Items);
            order.Items.Clear();
            decimal subtotal = 0;
            foreach (var requested in items)
            {
                var product = products[requested.ProductId];
                var line = new OrderItem
                {
                    ProductId = product.Id,
                    QuantityKg = requested.QuantityKg,
                    PricePerKgSnapshot = product.PricePerKg,
                    TotalPrice = product.PricePerKg * requested.QuantityKg,
                    ProductNameSnapshot = product.Name,
                    UnitSnapshot = string.IsNullOrWhiteSpace(product.Unit) ? "KG" : product.Unit.Trim()
                };
                order.Items.Add(line);
                subtotal += line.TotalPrice;
            }

            order.PickupSlotId = slot.Id;
            order.MarketId = slot.MarketId;
            order.PickupTime = NormalizePickupTime(pickupTime);
            order.SubTotal = subtotal;
            order.TotalAmount = subtotal;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return true;
    }

    private async Task EnsureSlotCapacityAsync(int slotId, int maxOrders)
    {
        if (maxOrders <= 0) throw new InvalidOperationException("This pickup slot is not accepting orders.");

        var activeCount = await _db.Orders.CountAsync(o =>
            o.PickupSlotId == slotId &&
            o.Status != OrderStatus.Cancelled &&
            o.Status != OrderStatus.Completed);

        if (activeCount >= maxOrders)
            throw new InvalidOperationException("The selected pickup slot is full. Please choose another slot.");
    }

    private static void ValidatePickupSelection(PickupSlot slot, DateTime pickupTime)
    {
        var local = pickupTime.Kind == DateTimeKind.Utc ? pickupTime.ToLocalTime() : pickupTime;
        if (local <= DateTime.Now.AddMinutes(-1))
            throw new InvalidOperationException("Pickup time must be in the future.");
        if (local.DayOfWeek != slot.DayOfWeek)
            throw new InvalidOperationException("The selected pickup date does not match the pickup slot day.");

        var time = TimeOnly.FromDateTime(local);
        if (time < slot.StartTime || time > slot.EndTime)
            throw new InvalidOperationException("The selected pickup time is outside the pickup slot window.");
    }

    private static DateTime NormalizePickupTime(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;

    private static bool IsValidTransition(OrderStatus current, OrderStatus next)
        => (current, next) switch
        {
            (OrderStatus.Pending, OrderStatus.Accepted) => true,
            (OrderStatus.Accepted, OrderStatus.ReadyForPickup) => true,
            (OrderStatus.ReadyForPickup, OrderStatus.Completed) => true,
            _ => false
        };

    private static bool CanModify(Order order)
    {
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Accepted)) return false;
        return !IsPastCutoff(order.PickupSlot, order.PickupTime);
    }

    private static bool CanCancel(Order order, string cancelledBy)
    {
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Accepted)) return false;
        return !IsPastCutoff(order.PickupSlot, order.PickupTime);
    }

    private static void ValidateProductQuantity(Product product, int quantity)
    {
        if (quantity <= 0) throw new InvalidOperationException($"Quantity for {product.Name} must be greater than zero.");
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

    private static bool IsPastCutoff(PickupSlot? slot, DateTime? pickupTime)
    {
        if (slot == null || pickupTime == null) return true;
        var local = pickupTime.Value.Kind == DateTimeKind.Utc ? pickupTime.Value.ToLocalTime() : pickupTime.Value;
        var cutoff = slot.CutoffTime ?? slot.StartTime;
        var cutoffAt = local.Date.Add(cutoff.ToTimeSpan());
        return DateTime.Now >= cutoffAt;
    }

    private async Task NotifyRestockedFavoritesAsync(IEnumerable<OrderItem> items)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        if (productIds.Count == 0) return;

        var favorites = await _db.Favorites
            .Include(f => f.Customer).ThenInclude(c => c.User)
            .Where(f => f.ProductId.HasValue && productIds.Contains(f.ProductId.Value))
            .ToListAsync();

        foreach (var favorite in favorites)
        {
            await _notificationService.SendAsync(
                favorite.Customer.UserId,
                NotificationType.LowStock,
                "A favourite product is available again",
                "A product you saved as a favourite has been restocked.",
                favorite.ProductId.HasValue ? $"/Products/Details/{favorite.ProductId.Value}" : null);
        }
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()[^6..];
        var random = Random.Shared.Next(1000, 9999);
        return $"ML-{timestamp}-{random}";
    }
}
