using MarketLink.Models;

namespace MarketLink.Areas.Admin.ViewModels
{
    /// <summary>
    /// Filter criteria for the Reports page. Every field is optional so the
    /// admin can drill down from an all-time summary to a specific slice.
    /// </summary>
    public class ReportFilterViewModel
    {
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DateTo { get; set; }

        public int? MarketId { get; set; }
        public int? FarmerId { get; set; }
        public OrderStatus? Status { get; set; }
    }

    /// <summary>
    /// Full Reports page payload. All figures are computed from the database
    /// via ReportService — no hardcoded/demo numbers.
    /// </summary>
    public class ReportsIndexViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new();

        public List<Market> Markets { get; set; } = new();
        public List<Farmer> Farmers { get; set; } = new();

        // Top-line KPIs
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }

        // Breakdowns
        public List<OrderStatusCount> OrderStatusBreakdown { get; set; } = new();
        public List<MarketOrderReportRow> OrdersByMarket { get; set; } = new();
        public List<FarmerOrderReportRow> OrdersByFarmer { get; set; } = new();
        public List<MonthlyRevenuePoint> RevenueByMonth { get; set; } = new();
        public List<TopProduct> TopProducts { get; set; } = new();
        public List<CustomerActivityRow> TopCustomers { get; set; } = new();
    }

    public class MarketOrderReportRow
    {
        public int MarketId { get; set; }
        public string MarketName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class FarmerOrderReportRow
    {
        public int FarmerId { get; set; }
        public string StallName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CustomerActivityRow
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    /// <summary>
    /// Analytics page payload. Focuses on visual trends rather than tabular
    /// report output, so the two pages complement each other.
    /// </summary>
    public class AnalyticsViewModel
    {
        public AnalyticsFilterViewModel Filter { get; set; } = new();

        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalFarmers { get; set; }
        public int TotalCustomers { get; set; }

        public List<MonthlyRevenuePoint> RevenueTrend { get; set; } = new();
        public List<MonthlyOrderPoint> OrderTrend { get; set; } = new();
        public List<OrderStatusCount> OrderStatusBreakdown { get; set; } = new();
        public List<MarketOrderReportRow> OrdersByMarket { get; set; } = new();
        public List<TopProduct> TopProducts { get; set; } = new();
        public List<TopFarmer> TopFarmers { get; set; } = new();
    }

    public class AnalyticsFilterViewModel
    {
        /// <summary>Number of months back from the current month (3, 6, or 12).</summary>
        public int Months { get; set; } = 6;
    }

    public class MonthlyOrderPoint
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int OrderCount { get; set; }
    }
}