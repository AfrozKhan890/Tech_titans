using MarketLink.Models;
using MarketLink.Services;
using MarketLink.Areas.Admin.Services;

namespace MarketLink.Data
{
    /// <summary>
    /// Seeds the MarketLink database with a development/demo Admin account
    /// and a handful of sample records so the Dashboard has real data to
    /// aggregate on first run.
    ///
    /// DEV/DEMO CREDENTIALS (documented per project requirements):
    ///   Email:    admin@marketlink.com
    ///   Password: Admin@123
    ///
    /// IMPORTANT: change this password before any real/production deployment.
    /// </summary>
    public static class DbInitializer
    {
        public static void Seed(MarketLinkDbContext context)
        {

            if (!context.AdminUsers.Any())
            {
                var (hash, salt) = PasswordHasher.HashPassword("Admin@123");
                context.AdminUsers.Add(new AdminUser
                {
                    FullName = "MarketLink Administrator",
                    Email = "admin@marketlink.com",
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Role = "SuperAdmin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!context.Markets.Any())
            {
                context.Markets.AddRange(
                    new Market
                    {
                        MarketName = "Riverside Farmers Market",
                        Address = "12 Riverside Ave, Springfield",
                        OperatingDays = "Sat, Sun",
                        OpeningTime = new TimeSpan(8, 0, 0),
                        ClosingTime = new TimeSpan(14, 0, 0),
                        Latitude = 39.781721m,
                        Longitude = -89.650148m,
                        MapProvider = "OpenStreetMap",
                        Status = MarketStatus.Active
                    },
                    new Market
                    {
                        MarketName = "Downtown Green Market",
                        Address = "45 Main St, Springfield",
                        OperatingDays = "Wed, Fri",
                        OpeningTime = new TimeSpan(9, 0, 0),
                        ClosingTime = new TimeSpan(13, 0, 0),
                        Latitude = 39.798363m,
                        Longitude = -89.644510m,
                        MapProvider = "OpenStreetMap",
                        Status = MarketStatus.Active
                    }
                );
                context.SaveChanges();
            }

            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Vegetables", Description = "Fresh seasonal vegetables" },
                    new Category { Name = "Fruits", Description = "Fresh seasonal fruits" },
                    new Category { Name = "Dairy", Description = "Milk, cheese, and dairy products" },
                    new Category { Name = "Baked Goods", Description = "Breads, pastries, and baked items" }
                );
                context.SaveChanges();
            }

            if (!context.Farmers.Any())
            {
                var riverside = context.Markets.First(m => m.MarketName == "Riverside Farmers Market");
                var downtown = context.Markets.First(m => m.MarketName == "Downtown Green Market");

                context.Farmers.AddRange(
                    new Farmer
                    {
                        StallName = "Green Acres Farm",
                        ContactPerson = "Sarah Miller",
                        Phone = "555-0101",
                        Email = "sarah@greenacres.example",
                        Address = "Route 9, Springfield",
                        Status = FarmerStatus.Approved,
                        OperatingDays = "Sat, Sun",
                        MarketId = riverside.MarketId,
                        RegisteredAt = DateTime.UtcNow.AddMonths(-4)
                    },
                    new Farmer
                    {
                        StallName = "Sunny Orchard",
                        ContactPerson = "Tom Reyes",
                        Phone = "555-0102",
                        Email = "tom@sunnyorchard.example",
                        Address = "Old Mill Rd, Springfield",
                        Status = FarmerStatus.Pending,
                        OperatingDays = "Wed, Fri",
                        MarketId = downtown.MarketId,
                        RegisteredAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new Farmer
                    {
                        StallName = "Hillside Dairy Co.",
                        ContactPerson = "Amina Yusuf",
                        Phone = "555-0103",
                        Email = "amina@hillsidedairy.example",
                        Address = "Hilltop Ln, Springfield",
                        Status = FarmerStatus.Approved,
                        OperatingDays = "Sat",
                        MarketId = riverside.MarketId,
                        RegisteredAt = DateTime.UtcNow.AddMonths(-8)
                    }
                );
                context.SaveChanges();
            }

            if (!context.Customers.Any())
            {
                context.Customers.AddRange(
                    new Customer { FullName = "Emily Carter", Email = "emily@example.com", Phone = "555-0201", Address = "10 Oak St", Status = CustomerStatus.Active, RegisteredAt = DateTime.UtcNow.AddMonths(-6) },
                    new Customer { FullName = "James Walker", Email = "james@example.com", Phone = "555-0202", Address = "22 Elm St", Status = CustomerStatus.Active, RegisteredAt = DateTime.UtcNow.AddMonths(-2) },
                    new Customer { FullName = "Priya Nair", Email = "priya@example.com", Phone = "555-0203", Address = "5 Birch Ave", Status = CustomerStatus.Inactive, RegisteredAt = DateTime.UtcNow.AddMonths(-10) }
                );
                context.SaveChanges();
            }

            if (!context.Products.Any())
            {
                var farmer = context.Farmers.First(f => f.StallName == "Green Acres Farm");
                var dairyFarmer = context.Farmers.First(f => f.StallName == "Hillside Dairy Co.");
                var veg = context.Categories.First(c => c.Name == "Vegetables");
                var dairy = context.Categories.First(c => c.Name == "Dairy");

                context.Products.AddRange(
                    new Product { FarmerId = farmer.FarmerId, CategoryId = veg.CategoryId, Name = "Heirloom Tomatoes", Price = 3.50m, Unit = "lb", StockQuantity = 40, Status = ProductStatus.Active },
                    new Product { FarmerId = farmer.FarmerId, CategoryId = veg.CategoryId, Name = "Organic Kale", Price = 2.75m, Unit = "bunch", StockQuantity = 25, Status = ProductStatus.Active },
                    new Product { FarmerId = dairyFarmer.FarmerId, CategoryId = dairy.CategoryId, Name = "Farmstead Cheddar", Price = 6.00m, Unit = "block", StockQuantity = 15, Status = ProductStatus.Active }
                );
                context.SaveChanges();
            }

            if (!context.Orders.Any())
            {
                var customer1 = context.Customers.First(c => c.FullName == "Emily Carter");
                var customer2 = context.Customers.First(c => c.FullName == "James Walker");
                var farmer = context.Farmers.First(f => f.StallName == "Green Acres Farm");
                var product1 = context.Products.First(p => p.Name == "Heirloom Tomatoes");
                var product2 = context.Products.First(p => p.Name == "Organic Kale");

                var order1 = new Order
                {
                    CustomerId = customer1.CustomerId,
                    FarmerId = farmer.FarmerId,
                    OrderStatus = OrderStatus.Completed,
                    OrderDate = DateTime.UtcNow.AddDays(-3),
                    PickupDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                    PickupTime = new TimeSpan(9, 30, 0),
                    TotalAmount = 10.50m
                };
                order1.OrderItems.Add(new OrderItem { ProductId = product1.ProductId, Quantity = 3, UnitPrice = 3.50m, LineTotal = 10.50m });

                var order2 = new Order
                {
                    CustomerId = customer2.CustomerId,
                    FarmerId = farmer.FarmerId,
                    OrderStatus = OrderStatus.Placed,
                    OrderDate = DateTime.UtcNow.AddHours(-6),
                    PickupDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    PickupTime = new TimeSpan(10, 0, 0),
                    TotalAmount = 5.50m
                };
                order2.OrderItems.Add(new OrderItem { ProductId = product2.ProductId, Quantity = 2, UnitPrice = 2.75m, LineTotal = 5.50m });

                context.Orders.AddRange(order1, order2);
                context.SaveChanges();
            }

            if (!context.Reviews.Any())
            {
                var customer1 = context.Customers.First(c => c.FullName == "Emily Carter");
                var farmer = context.Farmers.First(f => f.StallName == "Green Acres Farm");
                var product1 = context.Products.First(p => p.Name == "Heirloom Tomatoes");

                context.Reviews.Add(new Review
                {
                    CustomerId = customer1.CustomerId,
                    FarmerId = farmer.FarmerId,
                    ProductId = product1.ProductId,
                    Rating = 5,
                    Comment = "Best tomatoes I've had all season!",
                    ReviewDate = DateTime.UtcNow.AddDays(-1)
                });
                context.SaveChanges();
            }


            // Backfill customer-facing fields without replacing existing Admin data.
            foreach (var category in context.Categories.Where(c => c.Slug == null))
                category.Slug = Slugify(category.Name);

            foreach (var product in context.Products.Where(p => p.Slug == null))
                product.Slug = Slugify(product.Name) + "-" + product.ProductId;

            var demoCustomer = context.Customers.FirstOrDefault(c => c.Email == "customer@marketlink.com");
            if (demoCustomer == null)
            {
                var (hash, salt) = PasswordHasher.HashPassword("Customer@123");
                context.Customers.Add(new Customer
                {
                    FullName = "MarketLink Customer",
                    Email = "customer@marketlink.com",
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Status = CustomerStatus.Active,
                    DefaultCity = "Springfield",
                    RegisteredAt = DateTime.UtcNow
                });
            }
            else if (string.IsNullOrWhiteSpace(demoCustomer.PasswordHash))
            {
                var (hash, salt) = PasswordHasher.HashPassword("Customer@123");
                demoCustomer.PasswordHash = hash;
                demoCustomer.PasswordSalt = salt;
                demoCustomer.Status = CustomerStatus.Active;
            }

            context.SaveChanges();
        }

        private static string Slugify(string value)
        {
            var chars = value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
            return new string(chars).Trim('-');
        }

    }
}
