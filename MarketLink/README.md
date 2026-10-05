# MarketLink – Farmers Marketplace (eGreen Basket)

> A modern, production-grade web application connecting local farmers with community shoppers for farm-fresh produce and scheduled cash-on-pickup pre-orders.

Built with **ASP.NET Core 10 MVC** (`.NET 10`), **Entity Framework Core**, **SQL Server**, **Bootstrap 5**, **Repository & Unit of Work Patterns**, and **REST APIs with OpenAPI/Swagger**.

---

## 🌟 Key Platform Features

### 🛒 Customer Experience
- **Public Produce Catalog**: Advanced search, multi-faceted filtering (categories, certified organic, season, price range, sorting).
- **Physical Markets with Map Coordinates**: Browse local farmers markets, then open a market's own page at `/Markets/Details/<id>` for its address, hours, open days, contact details, map pin, the approved farms that stall there, the produce they have available for pickup, and the pickup windows published for that location.
- **Cash-on-Pickup Pre-Orders**: Build a cart, select specific pickup dates and farmer time windows, with physical cash payment at pickup (no online gateway needed).
- **Customer Dashboard**:
  - Saved favorites synchronized between navbar and dashboard.
  - Multi-address management (Home, Work, Other) with default selector.
  - Complete order history with status tracking and 1-click reordering.
  - Product & farmer review submissions with 5-star ratings.
  - In-app notification center.
  - Responsive collapsible sidebar with fixed brand header.

### 🚜 Farmer Vendor Module
- **Self Service Sign Up**: A seller registers at `/Auth/Register?role=Farmer`, is signed in immediately, and lands in the vendor console with the farm flagged **Pending**.
- **Approval Gated Catalog**: Produce, prices, stock, reviews and pickup windows added by a seller stay invisible to shoppers until an administrator approves the farm from `/Admin/Farmers`. The rule is enforced in the services layer, so both the pages and the `/api/*` responses honour it.
- **Farmer Dashboard**: KPI cards for active orders, today's pickups, low stock alerts, and monthly earnings.
- **Produce Management**: Full CRUD for produce items with units (kg, lb, bunch, dozen), pricing, stock tracking, and organic badges.
- **Pickup Slots & Capacity**: Schedule recurring weekly market stalls and maximum pre-order capacities.
- **Order Processing Workflow**: Review incoming pre-orders, Accept or Reject with reason, mark Ready for Pickup, and complete handoff.
- **Sales Analytics**: Visual sales trends, revenue summaries, and top-selling produce.
- **Customer Feedback**: Review customer ratings and post direct responses.

### 🛡️ Administrator Module
- **System Dashboard**: High-level platform KPIs, total orders, transaction volume, and recent audit logs.
- **Farmer Moderation**: Review vendor applications, approve, reject, or suspend farmer accounts.
- **Market & Stall Management**: Add physical market venues with GPS coordinates, hours, and open days.
- **Category Tree Management**: Maintain main produce categories and nested sub-categories with display order and icons.
- **Customer Oversight**: Inspect customer activity and toggle account statuses.
- **Audit & Review Moderation**: Moderate customer reviews and remove inappropriate content.
- **System Reports**: Exportable analytics on pre-orders, revenue, and farmer performance.

### 🌐 REST API & OpenAPI / Swagger
- Fully featured REST endpoints under `/api/*` for mobile apps and integrations.
- Interactive documentation at **`/swagger`**.

---

## 🏛️ Architecture & Project Structure

MarketLink is a **single MVC project** — the conventional `dotnet new mvc` shape, one `.csproj`, one solution file. The domain, data and presentation concerns are separated by folder and namespace rather than by project, so there is nothing to wire together before the app runs.

```
MarketLink/
│
├── Controllers/                     # Public MVC controllers (Home, Products, Farmers, Markets, Auth)
│   └── Api/                         # REST API controllers (Auth, Products, Orders, Markets, Reviews, …)
│
├── Areas/
│   ├── Admin/                       # Controllers + Views: dashboard, farmers, customers, markets, categories, reports
│   ├── Farmer/                      # Controllers + Views: dashboard, produce, orders, pickup slots, analytics, profile
│   └── Customer/                    # Controllers + Views: dashboard, cart, orders, favorites, profile, notifications
│
├── Models/                          # Domain entities (Product, Farmer, Customer, Order, Market, Review, …)
│   ├── Enums/                       # OrderStatus, FarmerStatus, Season, NotificationType
│   └── ViewModels/                  # DTOs and presentation models
│
├── Data/
│   ├── ApplicationDbContext.cs      # EF Core context, entity configurations, indexes
├── Migrations/                      # EF Core migrations
├── Data/SeedData/DatabaseSeeder.cs  # Startup demo seeder
├── database/seed-demo-data.sql      # Manual SQL demo seed
│
├── Repositories/                    # IRepository<T> / IUnitOfWork and their EF Core implementations
├── Services/                        # Domain services: cart, orders, products, farmers, notifications, image store
├── Views/                           # Shared layout, error page and public Razor views
├── wwwroot/                         # CSS, JS, vendor libraries, images and /uploads/products
└── docs/                            # Architectural documentation & UML diagrams
    ├── DATABASE_ER_DIAGRAM.md       # Entity-Relationship diagram (Mermaid)
    ├── USE_CASE_DIAGRAM.md          # Use Case diagram & actor matrix
    ├── CLASS_DIAGRAM.md             # Class hierarchy & folder layering
    ├── ACTIVITY_DIAGRAM.md          # Pre-order lifecycle sequence & state machine
    ├── API_DOCUMENTATION.md         # REST API endpoints & request/response schemas
    ├── DEPLOYMENT_GUIDE.md          # IIS, Azure, SmarterASP, and Docker guides
    └── TEST_ACCOUNTS.md             # Pre-configured test credentials
```

Namespaces mirror the folders: `MarketLink.Models`, `MarketLink.Models.Enums`, `MarketLink.Data`,
`MarketLink.Repositories`, `MarketLink.Services`, `MarketLink.Controllers`, `MarketLink.Areas.<Area>`.
`Program.cs` registers `IUnitOfWork`, the generic `Repository<>` and each service as scoped
dependencies, then runs migrations and the seeder on startup.

---

## 🚀 Quick Start Guide

### 1. Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or higher.
- [SQL Server](https://www.microsoft.com/sql-server/) (LocalDB, Express, or full instance).

### 2. Configure Connection String
Open `appsettings.json` in the project root and set your SQL Server connection:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MarketLinkDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 3. Build & Run
From the project root (the folder that contains `MarketLink.csproj`):

```bash
dotnet restore
dotnet build
dotnet run
```

The database is automatically migrated and populated with demo accounts, markets, products, pickup slots, inventory, favorites, orders, reviews, notifications, and announcements on startup via `DatabaseSeeder`. A standalone `database/seed-demo-data.sql` file is also included for manual SQL seeding.

Open your browser at `http://localhost:5229` (or `https://localhost:7089` with the `https` profile).

If another copy of the app is still running, startup stops with `MarketLink could not start: http://localhost:5229 is already in use.` instead of a Kestrel stack trace — close the other window, or use a spare port with `dotnet run --urls http://localhost:5231`.

Produce photos uploaded by sellers and administrators are written to `wwwroot/uploads/products/` and served back from `/uploads/products/…`. Uploads are capped at 4 MB and restricted to JPG, PNG and WEBP, verified by file signature rather than the client supplied content type.

### 4. Helper scripts
| Script | What it does |
| :--- | :--- |
| `scripts/verify-mvc.sh` | Curls the running app through 62 assertions: public pages, three-role sign-in, a link crawl of every dashboard, favourites, cart → checkout → seller fulfilment, notifications. Run it with `BASE_URL=http://localhost:5231 bash scripts/verify-mvc.sh`. It creates a completed order, so re-seed afterwards for a pristine demo database. |
| `scripts/make-favicon.ps1` | Redraws `wwwroot/favicon.ico` (16/24/32/48 px) from the same vector geometry as `wwwroot/favicon.svg`. Windows PowerShell + System.Drawing. |

Brand artwork lives in `wwwroot/images/` as hand-authored SVG (`hero-produce.svg`, `placeholder.svg`) so it stays crisp at any zoom and costs a few kilobytes; `placeholder.svg` is the fallback for any produce item without a photo.

---

## 🔑 Demo Accounts

Refer to [`docs/TEST_ACCOUNTS.md`](docs/TEST_ACCOUNTS.md) for full credentials.

| Role | Email | Password | Access Area |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@marketlink.com` | `Admin@123456` | `/Admin` |
| **Farmer** | `farmer1@marketlink.com` | `Farmer@123456` | `/Farmer` |
| **Customer** | `customer1@marketlink.com` | `Customer@123456` | `/Customer` |

---

## 📚 Finalization Documentation
- **[Installation Guide](docs/INSTALLATION_GUIDE.md)**
- **[Test Accounts](docs/TEST_ACCOUNTS.md)**
- **[Database SQL Deliverable Notes](database/README.md)**

## 📚 Technical Documentation & Diagrams
- **[Database ER Diagram](docs/DATABASE_ER_DIAGRAM.md)**
- **[Use Case Diagram](docs/USE_CASE_DIAGRAM.md)**
- **[Class & Architecture Diagram](docs/CLASS_DIAGRAM.md)**
- **[Activity & Fulfillment Sequence](docs/ACTIVITY_DIAGRAM.md)**
- **[REST API Specifications](docs/API_DOCUMENTATION.md)**
- **[Production Deployment Guide](docs/DEPLOYMENT_GUIDE.md)**
- **[Test Accounts Reference](docs/TEST_ACCOUNTS.md)**

---

## 📱 Responsive Design
The application uses responsive Bootstrap layouts across the public catalog, checkout, Customer Dashboard, Farmer Console, and Admin Portal. The final audit environment could not execute browser/device tests because the .NET SDK was unavailable, so runtime responsive verification is not claimed here.
