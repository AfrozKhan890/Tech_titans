namespace MarketLink.Areas.Customer
{
    public static class CustomerAuthDefaults
    {
        public const string AuthenticationScheme = "CustomerAuth";
        public const string CustomerPolicy = "CustomerOnly";
        public const string BasePath = "/Customer";
        public static readonly string[] CustomerRoles = { "Customer" };
    }
}
