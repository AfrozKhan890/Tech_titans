using MarketLink.Models;
using MarketLink.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Data.SeedData;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Admin", "Farmer", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await SeedSettingsAsync(db);
        await SeedCategoriesAsync(db);
        await SeedMarketsAsync(db);
        await SeedUsersAndProfilesAsync(db, userManager);
        // Demo products, inventory, favorites, orders and reviews are no longer seeded:
        // farmers add their own products and customers create their own favorites/orders/reviews.
        await SeedPickupSlotsAsync(db);
        await SeedNotificationsAsync(db);
        await SeedAnnouncementsAsync(db);
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext db)
    {
        if (!await db.SiteSettings.AnyAsync())
        {
            db.SiteSettings.AddRange(
                new SiteSetting { Key = "SiteName", Value = "MarketLink – eGreen Basket", Group = "General" },
                new SiteSetting { Key = "SiteTagline", Value = "Farm Fresh, Picked Local", Group = "General" },
                new SiteSetting { Key = "ContactEmail", Value = "info@marketlink.com", Group = "Contact" },
                new SiteSetting { Key = "ContactPhone", Value = "+92 300 1234567", Group = "Contact" },
                new SiteSetting { Key = "MaintenanceMode", Value = "false", Group = "System" });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext db)
    {
        var roots = new[]
        {
            ("Vegetables", "vegetables", "fas fa-carrot", 1),
            ("Fruits", "fruits", "fas fa-apple-alt", 2),
            ("Dairy", "dairy", "fas fa-cheese", 3),
            ("Herbs", "herbs", "fas fa-leaf", 4),
            ("Grains", "grains", "fas fa-seedling", 5)
        };

        foreach (var item in roots)
        {
            if (!await db.Categories.AnyAsync(c => c.Slug == item.Item2))
                db.Categories.Add(new Category { Name = item.Item1, Slug = item.Item2, IconClass = item.Item3, SortOrder = item.Item4, IsActive = true });
        }
        await db.SaveChangesAsync();

        var children = new Dictionary<string, (string Name, string Slug)[]>
        {
            ["vegetables"] = [("Tomato", "tomato"), ("Potato", "potato"), ("Onion", "onion"), ("Carrot", "carrot"), ("Spinach", "spinach"), ("Cucumber", "cucumber")],
            ["fruits"] = [("Mango", "mango"), ("Apple", "apple"), ("Banana", "banana"), ("Orange", "orange"), ("Guava", "guava")],
            ["dairy"] = [("Milk", "milk"), ("Yogurt", "yogurt"), ("Cheese", "cheese")],
            ["herbs"] = [("Basil", "basil"), ("Coriander", "coriander"), ("Mint", "mint")],
            ["grains"] = [("Wheat", "wheat"), ("Rice", "rice"), ("Corn", "corn")]
        };

        var childIcons = new Dictionary<string, string>
        {
            ["tomato"] = "fas fa-apple-alt", ["potato"] = "fas fa-seedling", ["onion"] = "fas fa-circle",
            ["carrot"] = "fas fa-carrot", ["spinach"] = "fas fa-leaf", ["cucumber"] = "fas fa-seedling",
            ["mango"] = "fas fa-lemon", ["apple"] = "fas fa-apple-alt", ["banana"] = "fas fa-carrot",
            ["orange"] = "fas fa-lemon", ["guava"] = "fas fa-apple-alt",
            ["milk"] = "fas fa-cheese", ["yogurt"] = "fas fa-cheese", ["cheese"] = "fas fa-cheese",
            ["basil"] = "fas fa-leaf", ["coriander"] = "fas fa-leaf", ["mint"] = "fas fa-leaf",
            ["wheat"] = "fas fa-seedling", ["rice"] = "fas fa-seedling", ["corn"] = "fas fa-seedling"
        };

        foreach (var pair in children)
        {
            var parent = await db.Categories.FirstAsync(c => c.Slug == pair.Key);
            var order = 1;
            foreach (var child in pair.Value)
            {
                if (!await db.Categories.AnyAsync(c => c.Slug == child.Slug))
                {
                    childIcons.TryGetValue(child.Slug, out var icon);
                    db.Categories.Add(new Category { Name = child.Name, Slug = child.Slug, ParentId = parent.Id, IconClass = icon ?? "fas fa-tag", SortOrder = order, IsActive = true });
                }
                order++;
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedMarketsAsync(ApplicationDbContext db)
    {
        var markets = new[]
        {
            new Market { Name = "Clifton Fresh Market", Address = "Boat Basin Road", City = "Karachi", State = "Sindh", Latitude = 24.8128, Longitude = 67.0309, OperatingHours = "7:00 AM - 2:00 PM", OpenDays = "Saturday,Sunday", Phone = "+92 300 1111111", Email = "clifton@marketlink.local", IsActive = true, IsFeatured = true, Description = "Weekend farmers market with fresh local produce." },
            new Market { Name = "Gulshan Growers Market", Address = "University Road", City = "Karachi", State = "Sindh", Latitude = 24.9193, Longitude = 67.0950, OperatingHours = "8:00 AM - 3:00 PM", OpenDays = "Wednesday,Saturday", Phone = "+92 300 2222222", Email = "gulshan@marketlink.local", IsActive = true, IsFeatured = true, Description = "Fresh vegetables, herbs, fruit and dairy from local growers." },
            new Market { Name = "DHA Community Market", Address = "Khayaban-e-Ittehad", City = "Karachi", State = "Sindh", Latitude = 24.8016, Longitude = 67.0662, OperatingHours = "8:00 AM - 2:00 PM", OpenDays = "Sunday", Phone = "+92 300 3333333", Email = "dha@marketlink.local", IsActive = true, IsFeatured = false, Description = "Neighbourhood pickup market for seasonal produce." },
            new Market { Name = "North Nazimabad Harvest Market", Address = "Hyderi Market Road", City = "Karachi", State = "Sindh", Latitude = 24.9436, Longitude = 67.0550, OperatingHours = "9:00 AM - 4:00 PM", OpenDays = "Saturday", Phone = "+92 300 4444444", Email = "north@marketlink.local", IsActive = true, IsFeatured = false, Description = "Community market featuring independent farmers." }
        };

        foreach (var market in markets)
        {
            if (!await db.Markets.AnyAsync(m => m.Name == market.Name))
                db.Markets.Add(market);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAndProfilesAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        var farmers = new[]
        {
            ("farmer1@marketlink.com", "Farmer1", "Harrison", "Harrison Organic Farms", "A1", "Clifton Fresh Market", 24.8132, 67.0314),
            ("sarah@sunnyside.com", "Sarah", "Miller", "Sunnyside Dairy Farm", "B3", "Gulshan Growers Market", 24.9198, 67.0954),
            ("miguel@miguelsfarm.com", "Miguel", "Rodriguez", "Miguel's Heritage Farm", "C7", "DHA Community Market", 24.8019, 67.0667),
            ("emma@herbgarden.com", "Emma", "Chen", "Emma's Herb Garden", "D2", "North Nazimabad Harvest Market", 24.9440, 67.0554),
            ("david@riverbanks.com", "David", "Thompson", "Riverbanks Fresh Produce", "E5", "Clifton Fresh Market", 24.8124, 67.0304)
        };

        foreach (var f in farmers)
        {
            var user = await userManager.FindByEmailAsync(f.Item1);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    FirstName = f.Item2,
                    LastName = f.Item3,
                    UserName = f.Item1,
                    Email = f.Item1,
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(user, "Farmer@123456");
                if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
                await userManager.AddToRoleAsync(user, "Farmer");
            }

            var farmer = await db.Farmers.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (farmer == null)
            {
                farmer = new Farmer
                {
                    UserId = user.Id,
                    FarmName = f.Item4,
                    Description = $"Fresh produce from {f.Item4}, prepared for weekly MarketLink pickup.",
                    StallNumber = f.Item5,
                    Phone = "+92 301 5550000",
                    Address = "Karachi, Sindh",
                    City = "Karachi",
                    State = "Sindh",
                    Latitude = f.Item7,
                    Longitude = f.Item8,
                    Status = FarmerStatus.Approved,
                    IsFeatured = true,
                    RegisteredAt = DateTime.UtcNow.AddDays(-60)
                };
                db.Farmers.Add(farmer);
                await db.SaveChangesAsync();
            }

        }

        var farmerRows = await db.Farmers.Include(f => f.User).ToListAsync();
        var marketRows = await db.Markets.ToListAsync();
        foreach (var farmer in farmerRows)
        {
            var targetName = farmer.FarmName switch
            {
                "Harrison Organic Farms" => "Clifton Fresh Market",
                "Sunnyside Dairy Farm" => "Gulshan Growers Market",
                "Miguel's Heritage Farm" => "DHA Community Market",
                "Emma's Herb Garden" => "North Nazimabad Harvest Market",
                _ => "Clifton Fresh Market"
            };
            var market = marketRows.First(m => m.Name == targetName);
            if (!await db.FarmerMarkets.AnyAsync(x => x.FarmerId == farmer.Id && x.MarketId == market.Id))
                db.FarmerMarkets.Add(new FarmerMarket { FarmerId = farmer.Id, MarketId = market.Id, StallNumber = farmer.StallNumber, IsActive = true });
        }
        await db.SaveChangesAsync();

        var customers = new[]
        {
            ("customer1@marketlink.com", "Customer1", "Johnson", "Clifton"),
            ("bob@example.com", "Bob", "Williams", "Gulshan"),
            ("carol@example.com", "Carol", "Davis", "DHA")
        };
        foreach (var c in customers)
        {
            var user = await userManager.FindByEmailAsync(c.Item1);
            if (user == null)
            {
                user = new ApplicationUser { FirstName = c.Item2, LastName = c.Item3, UserName = c.Item1, Email = c.Item1, EmailConfirmed = true, IsActive = true };
                var result = await userManager.CreateAsync(user, "Customer@123456");
                if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
                await userManager.AddToRoleAsync(user, "Customer");
            }
            var customer = await db.Customers.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (customer == null)
            {
                customer = new Customer { UserId = user.Id, DefaultCity = "Karachi" };
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            }
            if (!await db.CustomerAddresses.AnyAsync(a => a.CustomerId == customer.Id))
            {
                db.CustomerAddresses.Add(new CustomerAddress { CustomerId = customer.Id, Label = "Home", AddressLine1 = "Street 12, Block 5", City = "Karachi", State = "Sindh", PostalCode = "75500", IsDefault = true });
                await db.SaveChangesAsync();
            }
        }

        var admin = await userManager.FindByEmailAsync("admin@marketlink.com");
        if (admin == null)
        {
            admin = new ApplicationUser { FirstName = "Admin", LastName = "Administrator", UserName = "admin@marketlink.com", Email = "admin@marketlink.com", EmailConfirmed = true, IsActive = true };
            var result = await userManager.CreateAsync(admin, "Admin@123456");
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    private static async Task SeedProductsAsync(ApplicationDbContext db)
    {
        var specs = new[]
        {
            ("Harrison Organic Farms", "Heirloom Tomatoes", "heirloom-tomatoes", "tomato", 420m, true, true, true, "Summer"),
            ("Harrison Organic Farms", "Organic Carrots", "organic-carrots", "carrot", 260m, true, false, true, "AllYear"),
            ("Harrison Organic Farms", "Baby Spinach", "baby-spinach", "spinach", 340m, true, false, false, "AllYear"),
            ("Sunnyside Dairy Farm", "Fresh Whole Milk", "fresh-whole-milk", "milk", 480m, true, true, true, "AllYear"),
            ("Sunnyside Dairy Farm", "Farm Yogurt", "farm-yogurt", "yogurt", 520m, true, false, true, "AllYear"),
            ("Sunnyside Dairy Farm", "Artisan Cheese", "artisan-cheese", "cheese", 950m, false, true, false, "AllYear"),
            ("Miguel's Heritage Farm", "Red Potatoes", "red-potatoes", "potato", 220m, true, false, true, "AllYear"),
            ("Miguel's Heritage Farm", "Sweet Mangoes", "sweet-mangoes", "mango", 390m, true, true, true, "Summer"),
            ("Miguel's Heritage Farm", "Farm Cucumbers", "farm-cucumbers", "cucumber", 240m, true, false, false, "Summer"),
            ("Emma's Herb Garden", "Fresh Basil", "fresh-basil", "basil", 700m, true, true, true, "AllYear"),
            ("Emma's Herb Garden", "Garden Mint", "garden-mint", "mint", 550m, true, false, false, "AllYear"),
            ("Emma's Herb Garden", "Fresh Coriander", "fresh-coriander", "coriander", 500m, true, false, true, "AllYear"),
            ("Riverbanks Fresh Produce", "Golden Bananas", "golden-bananas", "banana", 310m, false, true, true, "AllYear"),
            ("Riverbanks Fresh Produce", "Crisp Apples", "crisp-apples", "apple", 450m, false, false, true, "Autumn"),
            ("Riverbanks Fresh Produce", "Fresh Oranges", "fresh-oranges", "orange", 360m, true, false, false, "Winter")
        };

        foreach (var s in specs)
        {
            var farmer = await db.Farmers.FirstAsync(f => f.FarmName == s.Item1);
            var category = await db.Categories.FirstAsync(c => c.Slug == s.Item4);
            var product = await db.Products.FirstOrDefaultAsync(p => p.Slug == s.Item3);
            if (product == null)
            {
                product = new Product
                {
                    FarmerId = farmer.Id,
                    CategoryId = category.Id,
                    Name = s.Item2,
                    Slug = s.Item3,
                    Description = $"Fresh {s.Item2.ToLowerInvariant()} from {farmer.FarmName}, prepared for local pickup.",
                    PricePerKg = s.Item5,
                    StockQuantityKg = 120,
                    ImageUrl = "/images/placeholder.svg",
                    Unit = "KG",
                    IsOrganic = s.Item6,
                    IsAvailable = true,
                    IsFeatured = s.Item7,
                    IsBestSeller = s.Item8,
                    Season = Enum.Parse<Season>(s.Item9),
                    AvailableQuantities = "[1,2,5,10,25,50]",
                    Tags = "fresh,local,pickup"
                };
                db.Products.Add(product);
                await db.SaveChangesAsync();
            }
            else
            {
                product.FarmerId = farmer.Id;
                product.CategoryId = category.Id;
                product.AvailableQuantities = "[1,2,5,10,25,50]";
                if (string.IsNullOrWhiteSpace(product.ImageUrl)) product.ImageUrl = "/images/placeholder.svg";
                product.IsAvailable = true;
                if (product.StockQuantityKg <= 0) product.StockQuantityKg = 120;
                await db.SaveChangesAsync();
            }

            if (!await db.ProductImages.AnyAsync(i => i.ProductId == product.Id))
            {
                db.ProductImages.Add(new ProductImage { ProductId = product.Id, ImageUrl = "/images/placeholder.svg", IsPrimary = true, SortOrder = 0 });
                await db.SaveChangesAsync();
            }
        }

        await db.Products.Where(p => p.AvailableQuantities == "[5,10,15,20,25,50]" || p.AvailableQuantities == "[1,5,10,25]")
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.AvailableQuantities, "[1,2,5,10,25,50]"));
    }

    private static async Task SeedPickupSlotsAsync(ApplicationDbContext db)
    {
        var farmers = await db.Farmers.ToListAsync();
        foreach (var farmer in farmers)
        {
            var market = await db.FarmerMarkets.Where(fm => fm.FarmerId == farmer.Id && fm.IsActive).Select(fm => fm.Market).FirstAsync();
            var days = market.OpenDays?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? ["Saturday"];
            var dayValues = days.Select(d => Enum.Parse<DayOfWeek>(d, true)).Distinct().Take(2).ToList();
            foreach (var day in dayValues)
            {
                if (!await db.PickupSlots.AnyAsync(s => s.FarmerId == farmer.Id && s.MarketId == market.Id && s.DayOfWeek == day))
                {
                    db.PickupSlots.Add(new PickupSlot
                    {
                        FarmerId = farmer.Id,
                        MarketId = market.Id,
                        DayOfWeek = day,
                        StartTime = new TimeOnly(8, 0),
                        EndTime = new TimeOnly(13, 0),
                        CutoffTime = new TimeOnly(7, 0),
                        MaxOrders = 30,
                        IsActive = true,
                        Notes = "Weekly pickup window"
                    });
                }
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedInventoryAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        var week = System.Globalization.ISOWeek.GetWeekOfYear(now);
        var year = System.Globalization.ISOWeek.GetYear(now);
        var products = await db.Products.ToListAsync();
        foreach (var p in products)
        {
            var exists = await db.Inventories.AnyAsync(i => i.ProductId == p.Id && i.FarmerId == p.FarmerId && i.WeekNumber == week && i.Year == year);
            if (!exists)
                db.Inventories.Add(new Inventory { ProductId = p.Id, FarmerId = p.FarmerId, WeekNumber = week, Year = year, IsRecurringTemplate = true, AvailableQuantityKg = p.StockQuantityKg, PricePerKg = p.PricePerKg });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedFavoritesAsync(ApplicationDbContext db)
    {
        var customers = await db.Customers.ToListAsync();
        var products = await db.Products.Where(p => p.IsAvailable).OrderBy(p => p.Id).Take(6).ToListAsync();
        var farmers = await db.Farmers.Where(f => f.Status == FarmerStatus.Approved).OrderBy(f => f.Id).Take(3).ToListAsync();
        var markets = await db.Markets.Where(m => m.IsActive).OrderBy(m => m.Id).Take(2).ToListAsync();
        foreach (var c in customers)
        {
            foreach (var p in products.Take(2)) if (!await db.Favorites.AnyAsync(f => f.CustomerId == c.Id && f.ProductId == p.Id)) db.Favorites.Add(new Favorite { CustomerId = c.Id, ProductId = p.Id });
            var farmer = farmers.ElementAtOrDefault(c.Id % Math.Max(farmers.Count, 1));
            if (farmer != null && !await db.Favorites.AnyAsync(f => f.CustomerId == c.Id && f.FarmerId == farmer.Id)) db.Favorites.Add(new Favorite { CustomerId = c.Id, FarmerId = farmer.Id });
            var market = markets.ElementAtOrDefault(c.Id % Math.Max(markets.Count, 1));
            if (market != null && !await db.Favorites.AnyAsync(f => f.CustomerId == c.Id && f.MarketId == market.Id)) db.Favorites.Add(new Favorite { CustomerId = c.Id, MarketId = market.Id });
        }
        await db.SaveChangesAsync();

        var alice = await db.Customers.Include(c => c.CartItems).FirstAsync(c => c.User.Email == "customer1@marketlink.com");
        var firstProduct = products.FirstOrDefault();
        if (firstProduct != null && !alice.CartItems.Any(i => i.ProductId == firstProduct.Id))
        {
            db.CartItems.Add(new CartItem { CustomerId = alice.Id, ProductId = firstProduct.Id, QuantityKg = 1 });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedOrdersAsync(ApplicationDbContext db)
    {
        if (await db.Orders.AnyAsync()) return;
        var alice = await db.Customers.FirstAsync(c => c.User.Email == "customer1@marketlink.com");
        var bob = await db.Customers.FirstAsync(c => c.User.Email == "bob@example.com");
        var farmers = await db.Farmers.ToListAsync();
        var products = await db.Products.OrderBy(p => p.Id).ToListAsync();
        var slots = await db.PickupSlots.ToListAsync();
        var now = DateTime.Now;

        for (var i = 0; i < 6; i++)
        {
            var farmer = farmers[i % farmers.Count];
            var product = products.First(p => p.FarmerId == farmer.Id);
            var slot = slots.First(s => s.FarmerId == farmer.Id);
            var customer = i % 2 == 0 ? alice : bob;
            var pickup = NextOccurrence(slot.DayOfWeek, slot.StartTime, now, i == 0 ? 0 : i + 1);
            var quantity = i % 3 == 0 ? 1 : i % 3 == 1 ? 2 : 5;
            var status = i switch { 0 => OrderStatus.Pending, 1 => OrderStatus.Accepted, 2 => OrderStatus.ReadyForPickup, _ => OrderStatus.Completed };
            var ordered = now.AddDays(-(6 - i));
            var completed = status == OrderStatus.Completed ? ordered.AddHours(6) : (DateTime?)null;
            var total = product.PricePerKg * quantity;
            var order = new Order
            {
                OrderNumber = $"ML-DEMO-{1001 + i}", CustomerId = customer.Id, FarmerId = farmer.Id, PickupSlotId = slot.Id, MarketId = slot.MarketId,
                PickupTime = pickup, Status = status, SubTotal = total, TotalAmount = total, PaymentMethod = "Cash on Pickup", OrderedAt = ordered, OrderDate = ordered,
                AcceptedAt = status is OrderStatus.Accepted or OrderStatus.ReadyForPickup or OrderStatus.Completed ? ordered.AddHours(1) : null,
                ReadyAt = status is OrderStatus.ReadyForPickup or OrderStatus.Completed ? ordered.AddHours(3) : null, CompletedAt = completed,
                Notes = "Demo order for testing the MarketLink workflow."
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            db.OrderItems.Add(new OrderItem { OrderId = order.Id, ProductId = product.Id, QuantityKg = quantity, PricePerKgSnapshot = product.PricePerKg, TotalPrice = total, ProductNameSnapshot = product.Name, UnitSnapshot = product.Unit });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedReviewsAsync(ApplicationDbContext db)
    {
        if (await db.Reviews.AnyAsync()) return;
        var completed = await db.Orders.Where(o => o.Status == OrderStatus.Completed).OrderBy(o => o.Id).Take(4).ToListAsync();
        var comments = new[] { "Fresh and well packed.", "Good quality and easy pickup.", "Very fresh produce.", "Exactly as listed." };
        foreach (var order in completed)
        {
            var item = await db.OrderItems.FirstAsync(i => i.OrderId == order.Id);
            db.Reviews.Add(new Review { CustomerId = order.CustomerId, ProductId = item.ProductId, FarmerId = order.FarmerId, OrderId = order.Id, Rating = 5, Title = "Great experience", Comment = comments[order.Id % comments.Length], IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-2) });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedNotificationsAsync(ApplicationDbContext db)
    {
        if (await db.Notifications.AnyAsync()) return;
        var users = await db.Users.Where(u => u.Email == "customer1@marketlink.com" || u.Email == "farmer1@marketlink.com" || u.Email == "admin@marketlink.com").ToListAsync();
        foreach (var user in users)
        {
            db.Notifications.Add(new Notification { UserId = user.Id, Type = NotificationType.General, Title = "Welcome to MarketLink", Message = "Your demo account is ready. Explore products, orders, pickup slots and dashboards.", Link = "/Home/Index", IsRead = false });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedAnnouncementsAsync(ApplicationDbContext db)
    {
        if (await db.Announcements.AnyAsync()) return;
        db.Announcements.Add(new Announcement { Title = "Fresh weekend pickup", Message = "New local produce is available for this week's pickup windows.", IsActive = true, StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(30) });
        await db.SaveChangesAsync();
    }

    private static DateTime NextOccurrence(DayOfWeek day, TimeOnly time, DateTime from, int offsetWeeks)
    {
        var days = ((int)day - (int)from.DayOfWeek + 7) % 7;
        var date = from.Date.AddDays(days).AddDays(offsetWeeks * 7);
        var candidate = date.Add(time.ToTimeSpan());
        if (candidate <= from) candidate = candidate.AddDays(7);
        return candidate;
    }
}
