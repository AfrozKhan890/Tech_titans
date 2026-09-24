namespace MarketLink.Models
{
    public enum FarmerStatus
    {
        Pending = 0,
        Approved = 1,
        Suspended = 2,
        Inactive = 3
    }

    public enum CustomerStatus
    {
        Active = 0,
        Inactive = 1
    }

    public enum MarketStatus
    {
        Active = 0,
        Inactive = 1
    }

    public enum ProductStatus
    {
        Active = 0,
        SoldOut = 1,
        Removed = 2,
        Inactive = 3
    }

    public enum OrderStatus
    {
        Placed = 0,
        Accepted = 1,
        ReadyForPickup = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum Season { AllYear, Spring, Summer, Autumn, Winter }

    public enum AnnouncementStatus
    {
        Draft = 0,
        Published = 1,
        Unpublished = 2
    }
}
