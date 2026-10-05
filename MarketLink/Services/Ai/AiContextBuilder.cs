using System.Text;
using System.Text.RegularExpressions;
using MarketLink.Data;
using MarketLink.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services.Ai;

















public class AiContextBuilder
{
    private readonly ApplicationDbContext _db;

    public AiContextBuilder(ApplicationDbContext db) => _db = db;


    private static readonly Regex CustomerIdRegex =
        new(@"\bcustomers?\s*#?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);


    private static readonly Regex FarmerIdRegex =
        new(@"\bfarmers?\s*#?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);


    private static readonly Regex ProductIdRegex =
        new(@"\bproducts?\s*#?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);


    private static readonly Regex OrderIdRegex =
        new(@"\borders?\s*#?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);



    private const int MaxLookupsPerMessage = 5;

    public async Task<string> BuildCustomerContextAsync(int customerId)
    {
        var cart = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        var recentOrders = await _db.Orders
            .Include(o => o.Farmer)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderedAt)
            .Take(5)
            .ToListAsync();

        var favorites = await _db.Favorites
            .Include(f => f.Product)
            .Include(f => f.Farmer)
            .Where(f => f.CustomerId == customerId)
            .ToListAsync();

        var markets = await _db.Markets
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Take(10)
            .ToListAsync();

        var products = await _db.Products
            .Include(p => p.Farmer)
            .Include(p => p.Category)
            .Where(p => p.IsAvailable && p.StockQuantityKg > 0 && p.Farmer.Status == FarmerStatus.Approved)
            .OrderByDescending(p => p.IsFeatured)
            .Take(20)
            .ToListAsync();

        var sb = new StringBuilder();

        sb.AppendLine("MY CART:");
        if (cart.Count == 0) sb.AppendLine("- (empty)");
        foreach (var c in cart)
            sb.AppendLine($"- {c.QuantityKg} {c.Product.Unit} of {c.Product.Name} @ ${c.Product.PricePerKg}/{c.Product.Unit}");

        sb.AppendLine("MY RECENT ORDERS:");
        if (recentOrders.Count == 0) sb.AppendLine("- none yet");
        foreach (var o in recentOrders)
        {
            sb.AppendLine($"- Order ID #{o.Id} / Order #{o.OrderNumber}: status={o.Status}, farm={o.Farmer.FarmName}, total=${o.TotalAmount:F2}, ordered {o.OrderedAt:yyyy-MM-dd}, pickup={o.PickupTime:yyyy-MM-dd HH:mm}");
            foreach (var item in o.Items)
                sb.AppendLine($"  - {item.QuantityKg} {item.UnitSnapshot} of {item.ProductNameSnapshot}");
        }

        sb.AppendLine("MY FAVORITES:");
        if (favorites.Count == 0) sb.AppendLine("- none yet");
        foreach (var f in favorites)
            sb.AppendLine(f.Product != null ? $"- Product: {f.Product.Name}" : f.Farmer != null ? $"- Farmer: {f.Farmer.FarmName}" : "- Market");

        sb.AppendLine("MARKETS CURRENTLY OPEN FOR LISTING:");
        foreach (var m in markets)
            sb.AppendLine($"- {m.Name}, {m.City}, {m.State} — hours: {m.OperatingHours ?? "see market page"}");

        sb.AppendLine("SAMPLE OF PRODUCTS CURRENTLY IN STOCK (not exhaustive):");
        foreach (var p in products)
            sb.AppendLine($"- {p.Name} ({p.Category?.Name}) — ${p.PricePerKg}/{p.Unit} — {p.StockQuantityKg} {p.Unit} left — sold by {p.Farmer.FarmName}");

        return sb.ToString();
    }

    public async Task<string> BuildFarmerContextAsync(int farmerId)
    {
        var farmer = await _db.Farmers.AsNoTracking().FirstOrDefaultAsync(f => f.Id == farmerId);

        var products = await _db.Products
            .Where(p => p.FarmerId == farmerId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var pendingOrders = await _db.Orders
            .Where(o => o.FarmerId == farmerId && o.Status == OrderStatus.Pending)
            .OrderBy(o => o.OrderedAt)
            .ToListAsync();

        var recentOrders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.FarmerId == farmerId)
            .OrderByDescending(o => o.OrderedAt)
            .Take(10)
            .ToListAsync();

        var completedOrders = await _db.Orders
            .Where(o => o.FarmerId == farmerId && o.Status == OrderStatus.Completed)
            .ToListAsync();

        var bestSellers = await _db.OrderItems
            .Where(oi => oi.Order.FarmerId == farmerId && oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => oi.ProductNameSnapshot)
            .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.QuantityKg) })
            .OrderByDescending(x => x.Qty)
            .Take(5)
            .ToListAsync();

        var slots = await _db.PickupSlots
            .Where(s => s.FarmerId == farmerId && s.IsActive)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .ToListAsync();

        var reviews = await _db.Reviews
            .Include(r => r.Product)
            .Where(r => r.FarmerId == farmerId || (r.Product != null && r.Product.FarmerId == farmerId))
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        var sb = new StringBuilder();

        sb.AppendLine($"MY FARM PROFILE: {farmer?.FarmName}, status={farmer?.Status}, markets I sell at are managed in my profile.");

        sb.AppendLine("MY PRODUCTS / STOCK:");
        if (products.Count == 0) sb.AppendLine("- no products listed yet");
        foreach (var p in products)
            sb.AppendLine($"- {p.Name}: {p.StockQuantityKg} {p.Unit} in stock, ${p.PricePerKg}/{p.Unit}, {(p.IsAvailable ? "available" : "marked unavailable")}");

        sb.AppendLine($"MY PENDING ORDERS ({pendingOrders.Count}):");
        foreach (var o in pendingOrders)
            sb.AppendLine($"- Order #{o.OrderNumber}: ${o.TotalAmount:F2}, ordered {o.OrderedAt:yyyy-MM-dd}");

        sb.AppendLine("MY RECENT ORDERS:");
        foreach (var o in recentOrders)
        {
            sb.AppendLine($"- Order ID #{o.Id} / Order #{o.OrderNumber}: status={o.Status}, ${o.TotalAmount:F2}");
            foreach (var item in o.Items)
                sb.AppendLine($"  - {item.QuantityKg} {item.UnitSnapshot} of {item.ProductNameSnapshot}");
        }

        sb.AppendLine($"MY REVENUE: ${completedOrders.Sum(o => o.TotalAmount):F2} across {completedOrders.Count} completed orders.");

        sb.AppendLine("MY BEST-SELLING PRODUCTS:");
        if (bestSellers.Count == 0) sb.AppendLine("- not enough completed sales yet");
        foreach (var b in bestSellers)
            sb.AppendLine($"- {b.Name}: {b.Qty} units sold");

        sb.AppendLine("MY PICKUP SLOTS:");
        if (slots.Count == 0) sb.AppendLine("- none configured yet");
        foreach (var s in slots)
            sb.AppendLine($"- {s.DayOfWeek} {s.StartTime}-{s.EndTime}, max {s.MaxOrders} orders, {(s.IsActive ? "active" : "inactive")}");

        sb.AppendLine("MY RECENT REVIEWS:");
        if (reviews.Count == 0) sb.AppendLine("- no reviews yet");
        foreach (var r in reviews)
            sb.AppendLine($"- {r.Rating}/5 on {(r.Product != null ? r.Product.Name : "my farm")}: \"{r.Comment}\"");

        return sb.ToString();
    }










    public async Task<string> BuildAdminContextAsync(string? adminMessage = null)
    {
        var totalFarmers = await _db.Farmers.CountAsync();
        var pendingFarmers = await _db.Farmers.CountAsync(f => f.Status == FarmerStatus.Pending);
        var suspendedFarmers = await _db.Farmers.CountAsync(f => f.Status == FarmerStatus.Suspended);
        var totalCustomers = await _db.Customers.CountAsync();
        var totalMarkets = await _db.Markets.CountAsync(m => m.IsActive);
        var totalOrders = await _db.Orders.CountAsync();
        var pendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var pendingOrderList = await _db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Farmer)
            .Where(o => o.Status == OrderStatus.Pending)
            .OrderBy(o => o.OrderedAt)
            .Take(20)
            .AsNoTracking()
            .ToListAsync();

        var totalRevenue = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        var unapprovedReviews = await _db.Reviews.CountAsync(r => !r.IsApproved);
        var categories = await _db.Categories.Where(c => c.IsActive).Select(c => c.Name).ToListAsync();

        var topFarmers = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .GroupBy(o => o.Farmer.FarmName)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync();

        var recentAnnouncements = await _db.Announcements
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => a.Title)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("PLATFORM-WIDE STATISTICS (aggregate):");
        sb.AppendLine($"- Total Farmers: {totalFarmers} (Pending approval: {pendingFarmers}, Suspended: {suspendedFarmers})");
        sb.AppendLine($"- Total Customers: {totalCustomers}");
        sb.AppendLine($"- Active Markets: {totalMarkets}");
        sb.AppendLine($"- Total Orders: {totalOrders} (Pending: {pendingOrders})");
        sb.AppendLine("PENDING ORDERS (up to 20 oldest):");
        if (pendingOrderList.Count == 0) sb.AppendLine("- none");
        foreach (var o in pendingOrderList)
            sb.AppendLine($"- Order ID #{o.Id} / Order #{o.OrderNumber}: customer={o.Customer.User.FullName} (Customer #{o.CustomerId}), farmer={o.Farmer.FarmName} (Farmer #{o.FarmerId}), total=${o.TotalAmount:F2}, ordered {o.OrderedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- Completed Revenue: ${totalRevenue:F2}");
        sb.AppendLine($"- Reviews awaiting moderation: {unapprovedReviews}");
        sb.AppendLine("TOP FARMERS BY COMPLETED REVENUE:");
        foreach (var f in topFarmers) sb.AppendLine($"- {f.Name}: ${f.Revenue:F2}");
        sb.AppendLine("PRODUCT CATEGORIES: " + (categories.Count == 0 ? "(none configured)" : string.Join(", ", categories)));
        sb.AppendLine("ACTIVE ANNOUNCEMENTS: " + (recentAnnouncements.Count == 0 ? "(none)" : string.Join("; ", recentAnnouncements)));

        var lookupSection = await BuildAdminRecordLookupSectionAsync(adminMessage);
        if (!string.IsNullOrEmpty(lookupSection))
        {
            sb.AppendLine();
            sb.Append(lookupSection);
        }

        return sb.ToString();
    }







    private async Task<string> BuildAdminRecordLookupSectionAsync(string? adminMessage)
    {
        if (string.IsNullOrWhiteSpace(adminMessage))
            return string.Empty;


        var requests = new List<(string Kind, int Id)>();
        void Collect(Regex regex, string kind)
        {
            foreach (Match m in regex.Matches(adminMessage))
            {
                if (!int.TryParse(m.Groups[1].Value, out var id) || id <= 0) continue;
                if (requests.Any(r => r.Kind == kind && r.Id == id)) continue;
                if (requests.Count >= MaxLookupsPerMessage) return;
                requests.Add((kind, id));
            }
        }

        Collect(CustomerIdRegex, "Customer");
        Collect(FarmerIdRegex, "Farmer");
        Collect(ProductIdRegex, "Product");
        Collect(OrderIdRegex, "Order");

        if (requests.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("SPECIFIC RECORD LOOKUP (only these records - do not describe any other customer, farmer, product, or order):");

        foreach (var (kind, id) in requests)
        {
            var block = kind switch
            {
                "Customer" => await BuildAdminCustomerLookupAsync(id),
                "Farmer" => await BuildAdminFarmerLookupAsync(id),
                "Product" => await BuildAdminProductLookupAsync(id),
                "Order" => await BuildAdminOrderLookupAsync(id),
                _ => $"{kind} #{id}: not found."
            };
            sb.AppendLine(block);
        }

        return sb.ToString();
    }

    private async Task<string> BuildAdminCustomerLookupAsync(int customerId)
    {
        var customer = await _db.Customers
            .Include(c => c.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null)
            return $"Customer #{customerId}: not found.";

        var orderCount = await _db.Orders.CountAsync(o => o.CustomerId == customerId);
        var recentOrders = await _db.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderedAt)
            .Take(5)
            .ToListAsync();
        var reviewCount = await _db.Reviews.CountAsync(r => r.CustomerId == customerId);
        var favoriteCount = await _db.Favorites.CountAsync(f => f.CustomerId == customerId);

        var sb = new StringBuilder();
        sb.AppendLine($"Customer #{customer.Id}:");
        sb.AppendLine($"  Name: {customer.User.FullName}");
        sb.AppendLine($"  Email: {customer.User.Email}");
        if (!string.IsNullOrWhiteSpace(customer.User.PhoneNumber))
            sb.AppendLine($"  Phone: {customer.User.PhoneNumber}");
        sb.AppendLine($"  Account active: {customer.User.IsActive}");
        sb.AppendLine($"  Registered: {customer.User.CreatedAt:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(customer.DefaultCity))
            sb.AppendLine($"  Default city: {customer.DefaultCity}");
        sb.AppendLine($"  Total orders: {orderCount}, Reviews written: {reviewCount}, Favorites saved: {favoriteCount}");
        sb.AppendLine("  Recent orders:");
        if (recentOrders.Count == 0) sb.AppendLine("    - none yet");
        foreach (var o in recentOrders)
            sb.AppendLine($"    - Order #{o.OrderNumber}: status={o.Status}, total=${o.TotalAmount:F2}, ordered {o.OrderedAt:yyyy-MM-dd}");

        return sb.ToString();
    }

    private async Task<string> BuildAdminFarmerLookupAsync(int farmerId)
    {
        var farmer = await _db.Farmers
            .Include(f => f.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == farmerId);

        if (farmer == null)
            return $"Farmer #{farmerId}: not found.";

        var products = await _db.Products
            .Where(p => p.FarmerId == farmerId)
            .OrderBy(p => p.Name)
            .ToListAsync();
        var orderCount = await _db.Orders.CountAsync(o => o.FarmerId == farmerId);
        var completedRevenue = await _db.Orders
            .Where(o => o.FarmerId == farmerId && o.Status == OrderStatus.Completed)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        var reviewCount = await _db.Reviews.CountAsync(r => r.FarmerId == farmerId || (r.Product != null && r.Product.FarmerId == farmerId));

        var sb = new StringBuilder();
        sb.AppendLine($"Farmer #{farmer.Id}:");
        sb.AppendLine($"  Farm name: {farmer.FarmName}");
        sb.AppendLine($"  Status: {farmer.Status}");
        sb.AppendLine($"  Contact: {farmer.User.FullName}, {farmer.User.Email}{(string.IsNullOrWhiteSpace(farmer.Phone) ? "" : ", " + farmer.Phone)}");
        if (!string.IsNullOrWhiteSpace(farmer.City))
            sb.AppendLine($"  Location: {farmer.City}{(string.IsNullOrWhiteSpace(farmer.State) ? "" : ", " + farmer.State)}");
        sb.AppendLine($"  Registered: {farmer.RegisteredAt:yyyy-MM-dd}");
        sb.AppendLine($"  Total orders: {orderCount}, Completed revenue: ${completedRevenue:F2}, Reviews: {reviewCount}");
        sb.AppendLine($"  Products ({products.Count}):");
        if (products.Count == 0) sb.AppendLine("    - no products listed yet");
        foreach (var p in products)
            sb.AppendLine($"    - #{p.Id} {p.Name}: {p.StockQuantityKg} {p.Unit} in stock, ${p.PricePerKg}/{p.Unit}, {(p.IsAvailable ? "available" : "unavailable")}");

        return sb.ToString();
    }

    private async Task<string> BuildAdminProductLookupAsync(int productId)
    {
        var product = await _db.Products
            .Include(p => p.Farmer)
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return $"Product #{productId}: not found.";

        var reviewCount = await _db.Reviews.CountAsync(r => r.ProductId == productId);
        var avgRating = await _db.Reviews.Where(r => r.ProductId == productId).Select(r => (double?)r.Rating).AverageAsync() ?? 0;
        var timesOrdered = await _db.OrderItems.CountAsync(oi => oi.ProductId == productId);
        var favoriteCount = await _db.Favorites.CountAsync(f => f.ProductId == productId);

        var sb = new StringBuilder();
        sb.AppendLine($"Product #{product.Id}:");
        sb.AppendLine($"  Name: {product.Name}");
        sb.AppendLine($"  Category: {product.Category?.Name ?? "uncategorized"}");
        sb.AppendLine($"  Sold by: {product.Farmer.FarmName} (Farmer #{product.FarmerId})");
        sb.AppendLine($"  Price: ${product.PricePerKg}/{product.Unit}, Stock: {product.StockQuantityKg} {product.Unit}");
        sb.AppendLine($"  Available: {product.IsAvailable}, Organic: {product.IsOrganic}, Featured: {product.IsFeatured}");
        sb.AppendLine($"  Listed since: {product.CreatedAt:yyyy-MM-dd}");
        sb.AppendLine($"  Appeared in {timesOrdered} order line item(s), favorited by {favoriteCount} customer(s)");
        sb.AppendLine($"  Reviews: {reviewCount}{(reviewCount > 0 ? $", average rating {avgRating:F1}/5" : "")}");

        return sb.ToString();
    }

    private async Task<string> BuildAdminOrderLookupAsync(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Farmer)
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return $"Order #{orderId}: not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Order #{order.Id} (Order Number {order.OrderNumber}):");
        sb.AppendLine($"  Status: {order.Status}");
        sb.AppendLine($"  Customer: {order.Customer.User.FullName} (Customer #{order.CustomerId})");
        sb.AppendLine($"  Farmer: {order.Farmer.FarmName} (Farmer #{order.FarmerId})");
        sb.AppendLine($"  Subtotal: ${order.SubTotal:F2}, Total: ${order.TotalAmount:F2}");
        sb.AppendLine($"  Ordered: {order.OrderedAt:yyyy-MM-dd HH:mm}");
        if (order.PickupTime.HasValue)
            sb.AppendLine($"  Pickup time: {order.PickupTime:yyyy-MM-dd HH:mm}");
        if (order.CancelledAt.HasValue)
            sb.AppendLine($"  Cancelled: {order.CancelledAt:yyyy-MM-dd}, reason: {order.CancellationReason ?? "not given"}");
        sb.AppendLine($"  Items ({order.Items.Count}):");
        foreach (var item in order.Items)
            sb.AppendLine($"    - {item.QuantityKg} {item.UnitSnapshot} of {item.ProductNameSnapshot} @ ${item.PricePerKgSnapshot}/{item.UnitSnapshot} = ${item.TotalPrice:F2}");

        return sb.ToString();
    }
}
