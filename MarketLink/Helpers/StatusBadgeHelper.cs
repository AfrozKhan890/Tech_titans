using MarketLink.Models;

namespace MarketLink.Helpers
{
    /// <summary>
    /// Central place that maps every status enum to the matching
    /// ml-badge-* CSS class defined in wwwroot/css/admin.css, so every
    /// list/detail view renders status badges consistently.
    /// </summary>
    public static class StatusBadgeHelper
    {
        public static string For(FarmerStatus status) => status switch
        {
            FarmerStatus.Pending => "ml-badge-pending",
            FarmerStatus.Approved => "ml-badge-approved",
            FarmerStatus.Suspended => "ml-badge-suspended",
            FarmerStatus.Inactive => "ml-badge-inactive",
            _ => "ml-badge-inactive"
        };

        public static string For(CustomerStatus status) => status switch
        {
            CustomerStatus.Active => "ml-badge-active",
            CustomerStatus.Inactive => "ml-badge-inactive",
            _ => "ml-badge-inactive"
        };

        public static string For(MarketStatus status) => status switch
        {
            MarketStatus.Active => "ml-badge-active",
            MarketStatus.Inactive => "ml-badge-inactive",
            _ => "ml-badge-inactive"
        };

        public static string For(ProductStatus status) => status switch
        {
            ProductStatus.Active => "ml-badge-active",
            ProductStatus.SoldOut => "ml-badge-pending",
            ProductStatus.Removed => "ml-badge-cancelled",
            ProductStatus.Inactive => "ml-badge-inactive",
            _ => "ml-badge-inactive"
        };

        public static string For(OrderStatus status) => status switch
        {
            OrderStatus.Placed => "ml-badge-placed",
            OrderStatus.Accepted => "ml-badge-accepted",
            OrderStatus.ReadyForPickup => "ml-badge-readyforpickup",
            OrderStatus.Completed => "ml-badge-completed",
            OrderStatus.Cancelled => "ml-badge-cancelled",
            _ => "ml-badge-inactive"
        };

        public static string For(AnnouncementStatus status) => status switch
        {
            AnnouncementStatus.Published => "ml-badge-approved",
            AnnouncementStatus.Draft => "ml-badge-pending",
            AnnouncementStatus.Unpublished => "ml-badge-inactive",
            _ => "ml-badge-inactive"
        };
    }
}
