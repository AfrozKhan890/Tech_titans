using Microsoft.EntityFrameworkCore;
using MarketLink.Models;

namespace MarketLink.Data
{
    public class MarketLinkDbContext : DbContext
    {
        public MarketLinkDbContext(DbContextOptions<MarketLinkDbContext> options) : base(options) { }

        public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
        public DbSet<Farmer> Farmers => Set<Farmer>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Market> Markets => Set<Market>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<PlatformReport> PlatformReports => Set<PlatformReport>();
        public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();

        // Customer-side data stored in the same MarketLink database.
        public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AdminUser>().HasIndex(a => a.Email).IsUnique();

            modelBuilder.Entity<Customer>(e =>
            {
                e.HasIndex(c => c.Email).IsUnique();
                e.Property(c => c.PasswordHash).HasMaxLength(500);
                e.Property(c => c.PasswordSalt).HasMaxLength(500);
                e.Property(c => c.Bio).HasMaxLength(500);

                e.HasMany(c => c.Orders)
                    .WithOne(o => o.Customer)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(c => c.Reviews)
                    .WithOne(r => r.Customer)
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Farmer>(e =>
            {
                e.HasIndex(f => f.Email).IsUnique();

                e.HasMany(f => f.Products)
                    .WithOne(p => p.Farmer)
                    .HasForeignKey(p => p.FarmerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(f => f.Orders)
                    .WithOne(o => o.Farmer)
                    .HasForeignKey(o => o.FarmerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(f => f.Reviews)
                    .WithOne(r => r.Farmer)
                    .HasForeignKey(r => r.FarmerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(f => f.Market)
                    .WithMany(m => m.Farmers)
                    .HasForeignKey(f => f.MarketId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Market>(e =>
            {
                e.Property(m => m.Latitude).HasPrecision(9, 6);
                e.Property(m => m.Longitude).HasPrecision(9, 6);
            });

            modelBuilder.Entity<Category>(e =>
            {
                e.HasMany(c => c.Products)
                    .WithOne(p => p.Category)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.Property(c => c.Slug).HasMaxLength(120);
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.Property(p => p.Price).HasPrecision(10, 2);

                e.Property(p => p.Slug).HasMaxLength(220);

                e.Property(p => p.Tags).HasMaxLength(1000);

                // IMPORTANT:
                // Existing database stores Product.Status as nvarchar
                // values such as "Active", not as an integer.
                e.Property(p => p.Status).HasConversion<string>();

                e.Property(p => p.Season).HasConversion<string>();

                e.HasMany(p => p.OrderItems)
                    .WithOne(i => i.Product)
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(p => p.Reviews)
                    .WithOne(r => r.Product)
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductImage>(e =>
            {
                e.HasKey(x => x.ProductImageId);

                e.HasOne(x => x.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Order>(e =>
            {
                e.HasIndex(o => o.OrderNumber);

                e.Property(o => o.TotalAmount).HasPrecision(10, 2);

                e.Property(o => o.OrderStatus).HasConversion<string>();

                e.HasOne(o => o.Market)
                    .WithMany()
                    .HasForeignKey(o => o.MarketId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(o => o.OrderItems)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(e =>
            {
                e.Property(i => i.UnitPrice).HasPrecision(10, 2);
                e.Property(i => i.LineTotal).HasPrecision(10, 2);
            });

            modelBuilder.Entity<Review>(e =>
            {
                e.Property(r => r.Rating).IsRequired();

                e.HasOne(r => r.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.Farmer)
                    .WithMany(f => f.Reviews)
                    .HasForeignKey(r => r.FarmerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CustomerAddress>(e =>
            {
                e.HasOne(a => a.Customer)
                    .WithMany(c => c.Addresses)
                    .HasForeignKey(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(a => new { a.CustomerId, a.IsDefault });
            });

            modelBuilder.Entity<CartItem>(e =>
            {
                e.HasIndex(c => new { c.CustomerId, c.ProductId })
                    .IsUnique();

                e.HasOne(c => c.Customer)
                    .WithMany(x => x.CartItems)
                    .HasForeignKey(c => c.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(c => c.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Favorite>(e =>
            {
                e.HasOne(f => f.Customer)
                    .WithMany(c => c.Favorites)
                    .HasForeignKey(f => f.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(f => f.Product)
                    .WithMany(p => p.Favorites)
                    .HasForeignKey(f => f.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(f => f.Farmer)
                    .WithMany(f => f.Favorites)
                    .HasForeignKey(f => f.FarmerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(f => new { f.CustomerId, f.ProductId });
                e.HasIndex(f => new { f.CustomerId, f.FarmerId });
            });

            modelBuilder.Entity<Notification>(e =>
            {
                e.HasIndex(n => new { n.UserId, n.CreatedAt });
            });

            modelBuilder.Entity<Announcement>()
                .Property(a => a.Status)
                .HasConversion<string>();

            modelBuilder.Entity<PlatformSettings>()
                .Property(x => x.SmtpPassword)
                .HasMaxLength(300);
        }
    }
}