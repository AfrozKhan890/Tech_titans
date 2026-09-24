using MarketLink.Models;

namespace MarketLink.Areas.Admin.ViewModels
{
    public class DashboardViewModel
    {
        // Top summary cards (explicitly required by the SRS)
        public int TotalFarmers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalMarkets { get; set; }
        public int TotalOrders { get; set; }

        // Supporting widgets
        public int PendingFarmerApprovals { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }

        public List<Order> RecentOrders { get; set; } = new();
        public List<Review> RecentReviews { get; set; } = new();
        public List<Farmer> PendingFarmers { get; set; } = new();

        public List<OrderStatusCount> OrderStatusBreakdown { get; set; } = new();
        public List<MonthlyRevenuePoint> RevenueTrend { get; set; } = new();
        public List<TopFarmer> MostActiveFarmers { get; set; } = new();
        public List<TopProduct> PopularProducts { get; set; } = new();
    }

    public class OrderStatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyRevenuePoint
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class TopFarmer
    {
        public string StallName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
    }

    public class TopProduct
    {
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
    }
}
