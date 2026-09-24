# MarketLink — Admin + Customer Integration

## Project

**MarketLink** is a single ASP.NET Core MVC application targeting **.NET 10.0**.

The Customer implementation from the supplied `MarketLink-User-Side.zip` was integrated into the existing Admin application rather than retained as a second application.

### Final structure

- `Areas/Admin` — existing Admin implementation, preserved
- `Areas/Customer` — integrated Customer MVC Area
- `Models` — one shared domain model set
- `Data/MarketLinkDbContext.cs` — one shared EF Core DbContext
- `Services` — existing shared/Admin services
- `Areas/Customer/Services` — Customer-only identity service
- `Migrations` — existing migrations plus Customer integration migration
- `wwwroot` — shared static assets

## Target Framework

`net10.0`

The original Admin project was already on .NET 10. The Customer source was also nominally on `net10.0`, but its dependency architecture used separate Core/Infrastructure/Web projects and EF/Identity configuration. Those duplicate projects were not carried into the final application.

## URLs

The final project includes the `launchSettings.json` from the supplied Customer implementation, so the configured development URLs are:

### HTTPS

- **Admin:** https://localhost:7089/Admin
- **Customer:** https://localhost:7089/Customer

### HTTP

- **Admin:** http://localhost:5229/Admin
- **Customer:** http://localhost:5229/Customer

Specific pages:

- Admin login: `/Admin/Account/Login`
- Admin dashboard: `/Admin`
- Customer login: `/Customer/Account/Login`
- Customer registration: `/Customer/Account/Register`
- Customer dashboard: `/Customer`
- Customer orders: `/Customer/Orders`
- Customer favorites: `/Customer/Favorites`
- Customer profile: `/Customer/Profile`

## Authentication

### Admin

- Scheme: `AdminAuth`
- Policy: `AdminOnly`
- Cookie: `MarketLink.Admin.Auth`
- Roles: `SuperAdmin`, `Admin`

The existing Admin authentication architecture was preserved.

### Customer

- Scheme: `CustomerAuth`
- Policy: `CustomerOnly`
- Cookie: `MarketLink.Customer.Auth`
- Role claim: `Customer`

Customer authentication uses the existing `Customers` table and the existing MarketLink PBKDF2 password hasher. It does **not** use the Admin cookie and does **not** use a separate Identity database.

The authorization policies explicitly bind each role to its own authentication scheme.

## Database

The final application uses:

- DbContext: `MarketLinkDbContext`
- Connection string: `MarketLinkConnection`
- Database: `MarketLinkDb`

No `CustomerDbContext` or `ApplicationDbContext` was retained.

Customer features use the same database as Admin-managed:

- Customers
- Farmers
- Markets
- Categories
- Products
- Orders
- Order items
- Reviews

Customer-only supporting tables are also stored in the same database:

- `CustomerAddresses`
- `CartItems`
- `Favorites`
- `Notifications`
- `ProductImages`

## Migration Changes

Added:

`20260924163000_CustomerIntegration`

It adds the Customer integration fields and tables without dropping existing Admin tables/data.

Notable schema additions include:

- Customer password/profile fields
- Customer addresses
- Cart items
- Favorites
- Notifications
- Product images
- Product slugs/search metadata
- Market/customer-facing metadata
- Order number/pickup/notification metadata
- Review metadata
- Farmer customer-facing fields

The migration is non-destructive and does not recreate the database.

## Shared Admin → Customer Data Flow

Customer-facing queries use the same entities and database rows managed by Admin.

Examples:

- Admin activates a Market → Customer market pages query that Market.
- Admin approves a Farmer → Customer farmer/product pages only expose approved farmers.
- Admin activates/removes a Product → Customer product browsing respects product status.
- Admin activates/deactivates a Customer → Customer login checks the Customer status.
- Admin-managed Categories → Customer product filtering uses the same Categories.
- Admin-managed Orders/Reviews → Customer history/review pages read the same records.

## Customer Features Integrated

- Customer registration/login/logout
- Customer dashboard
- Product browsing
- Product search/filtering/sorting
- Farmer discovery
- Market browsing
- Product details
- Shopping cart
- Checkout/pre-orders
- Order history
- Order details
- Reorder
- Order cancellation
- Favorites for products/farmers
- Customer profile
- Saved addresses
- Notifications
- Customer reviews/ratings
- Responsive Customer dashboard UI

Customer checkout no longer uses the supplied implementation's hardcoded pickup-slot ID or hardcoded market list. Active markets are loaded from the shared database.

## Important Files Changed

- `Program.cs`
  - Added CustomerAuth
  - Added CustomerOnly policy
  - Added Customer routing
  - Preserved AdminAuth/AdminOnly
  - Uses the shared database migration pipeline
- `Data/MarketLinkDbContext.cs`
  - Extended the existing context for Customer features
- `Data/DbInitializer.cs`
  - Preserved Admin/demo data
  - Adds a Customer demo account
  - Backfills Customer-facing slugs/fields
- `Models/*`
  - Existing Admin domain models retained
  - Customer-facing compatibility fields and Customer-only entities added
- `Areas/Customer/*`
  - Added Customer MVC Area, authentication, controllers, services, views
- `Controllers/*`
  - Integrated public marketplace controllers against the shared DbContext
- `wwwroot/css/site.css`
  - Customer marketplace/dashboard styling
- `wwwroot/js/site.js`
  - Customer cart/favorites/notification interactions
- `Properties/launchSettings.json`
  - Configured development HTTP/HTTPS ports

## Removed / Not Carried Into Final Project

The following duplicate Customer application architecture was intentionally not copied into the final project:

- `MarketLink.Core` project
- `MarketLink.Infrastructure` project
- `MarketLink.Web` project
- Customer-side `ApplicationDbContext`
- Customer-side Identity database/schema
- Customer-side duplicate entity classes
- Customer-side duplicate repository/unit-of-work architecture
- Generated `bin/`
- Generated `obj/`
- `node_modules/`

These were replaced/merged into the existing MarketLink application so there is one MVC application and one DbContext.

The old root Customer authentication views/controllers from the supplied Customer project were also consolidated into `Areas/Customer/AccountController` and `Areas/Customer/Views/Account`. A small root `/Auth/*` compatibility controller remains so existing public navigation links continue to work.

## Test Accounts

### Admin

- Email: `admin@marketlink.com`
- Password: `Admin@123`

### Customer

- Email: `customer@marketlink.com`
- Password: `Customer@123`

The Customer demo account is created only if it does not already exist. Existing Customer records are not deleted.

Do not use these demo credentials in production.

## Run Instructions

From the project directory:

```text
cd D:\MarketLink
dotnet restore
dotnet build
dotnet run
```

Then open the configured HTTPS URL:

```text
https://localhost:7089
```

### Database prerequisite

The configured connection string is:

```text
Server=localhost;Database=MarketLinkDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

A SQL Server instance must therefore be available at `localhost` with Windows authentication enabled.

On startup the application runs EF Core migrations and then the existing seed initializer.

## Validation Status

### Source-level validation completed

- Admin and Customer projects were compared.
- Duplicate Customer DbContext architecture was removed.
- Customer references to the old Core/Infrastructure projects were removed.
- Customer routes use the Customer Area.
- Admin routes remain `/Admin`.
- Customer and Admin authentication schemes are separate.
- Customer/Admin authorization policies are scheme-specific.
- Customer checkout market selection is database-backed.
- Customer demo fallbacks were removed from integrated controllers.
- Customer-only schema changes are represented by an EF migration.
- `bin/`, `obj/`, and `node_modules/` are excluded from the final package.
- Basic C# brace-balance and stale-namespace scans were performed.

### Build/runtime limitation

The execution environment used to prepare this archive does **not** have the `dotnet` SDK/runtime installed, and no SQL Server instance is available here. Therefore an actual `dotnet restore`, `dotnet build`, or `dotnet run` could not be executed in this environment.

This is intentionally documented rather than claiming a build/runtime pass that was not performed.

Before deployment, run:

```text
cd D:\MarketLink
dotnet restore
dotnet build
dotnet run
```

Then verify:

1. `/Admin/Account/Login`
2. `/Admin`
3. `/Customer/Account/Login`
4. `/Customer`
5. Customer registration
6. Customer cart/checkout
7. Customer order history
8. Customer favorites
9. Customer profile/address management
10. Admin CRUD against the same database
11. Customer → Admin access is denied
12. Admin → Customer access is denied unless the Admin separately authenticates as a Customer
