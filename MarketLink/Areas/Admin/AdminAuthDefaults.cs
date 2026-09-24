namespace MarketLink.Areas.Admin
{
    /// <summary>
    /// Central place for the Admin area's authentication scheme name and
    /// authorization policy name.
    ///
    /// The Admin area uses its own NAMED cookie scheme ("AdminAuth") rather
    /// than the ASP.NET Core default scheme. This is deliberate: when the
    /// Farmer and Customer areas are added later, each will register its
    /// own named scheme (e.g. "FarmerAuth", "CustomerAuth") with its own
    /// login/logout paths and its own cookie. Because every Admin controller
    /// explicitly requires AdminAuthDefaults.AuthenticationScheme, a Farmer
    /// or Customer cookie can never accidentally satisfy [Authorize] on an
    /// Admin controller (and vice versa) — the three roles stay fully
    /// isolated even though they all live in one application.
    /// </summary>
    public static class AdminAuthDefaults
    {
        /// <summary>Name of the cookie authentication scheme used for Admin users.</summary>
        public const string AuthenticationScheme = "AdminAuth";

        /// <summary>Authorization policy requiring an authenticated user with an admin role.</summary>
        public const string AdminPolicy = "AdminOnly";

        /// <summary>Roles that satisfy <see cref="AdminPolicy"/>.</summary>
        public static readonly string[] AdminRoles = { "SuperAdmin", "Admin" };

        /// <summary>
        /// Fixed URL prefix the Admin area is served under.
        /// </summary>
        public const string BasePath = "/Admin";
    }
}
