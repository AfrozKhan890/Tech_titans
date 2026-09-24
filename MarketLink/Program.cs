using MarketLink.Areas.Admin;
using MarketLink.Areas.Customer;
using MarketLink.Areas.Customer.Services;
using MarketLink.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MarketLinkDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MarketLinkConnection")));

builder.Services.AddDataProtection();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CustomerAuthDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CustomerAuthDefaults.AuthenticationScheme;
    })
    .AddCookie(AdminAuthDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = AdminAuthDefaults.BasePath + "/Account/Login";
        options.LogoutPath = AdminAuthDefaults.BasePath + "/Account/Logout";
        options.AccessDeniedPath = AdminAuthDefaults.BasePath + "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "MarketLink.Admin.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddCookie(CustomerAuthDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = CustomerAuthDefaults.BasePath + "/Account/Login";
        options.LogoutPath = CustomerAuthDefaults.BasePath + "/Account/Logout";
        options.AccessDeniedPath = CustomerAuthDefaults.BasePath + "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "MarketLink.Customer.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminAuthDefaults.AdminPolicy, policy =>
        policy.AddAuthenticationSchemes(AdminAuthDefaults.AuthenticationScheme)
              .RequireAuthenticatedUser()
              .RequireRole(AdminAuthDefaults.AdminRoles));

    options.AddPolicy(CustomerAuthDefaults.CustomerPolicy, policy =>
        policy.AddAuthenticationSchemes(CustomerAuthDefaults.AuthenticationScheme)
              .RequireAuthenticatedUser()
              .RequireRole(CustomerAuthDefaults.CustomerRoles));
});

// Existing shared/Admin services — preserved.
builder.Services.AddScoped<MarketLink.Services.IFarmerService, MarketLink.Services.FarmerService>();
builder.Services.AddScoped<MarketLink.Services.ICustomerService, MarketLink.Services.CustomerService>();
builder.Services.AddScoped<MarketLink.Services.IMarketService, MarketLink.Services.MarketService>();
builder.Services.AddScoped<MarketLink.Services.IProductService, MarketLink.Services.ProductService>();
builder.Services.AddScoped<MarketLink.Services.IOrderService, MarketLink.Services.OrderService>();
builder.Services.AddScoped<MarketLink.Services.IReviewService, MarketLink.Services.ReviewService>();
builder.Services.AddScoped<MarketLink.Services.ICategoryService, MarketLink.Services.CategoryService>();
builder.Services.AddScoped<MarketLink.Services.IAnnouncementService, MarketLink.Services.AnnouncementService>();

builder.Services.AddScoped<MarketLink.Areas.Admin.Services.IDashboardService, MarketLink.Areas.Admin.Services.DashboardService>();
builder.Services.AddScoped<MarketLink.Areas.Admin.Services.IAdminAuthService, MarketLink.Areas.Admin.Services.AdminAuthService>();
builder.Services.AddScoped<MarketLink.Areas.Admin.Services.IAdminProfileService, MarketLink.Areas.Admin.Services.AdminProfileService>();
builder.Services.AddScoped<MarketLink.Areas.Admin.Services.IAnalyticsService, MarketLink.Areas.Admin.Services.AnalyticsService>();
builder.Services.AddScoped<MarketLink.Areas.Admin.Services.IReportService, MarketLink.Areas.Admin.Services.ReportService>();
builder.Services.AddScoped<MarketLink.Areas.Admin.Services.ISettingsService, MarketLink.Areas.Admin.Services.SettingsService>();

// Customer-only services operate on the same MarketLinkDbContext.
builder.Services.AddScoped<ICustomerIdentityService, CustomerIdentityService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" });

app.MapControllerRoute(
    name: "Customer",
    pattern: "Customer/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Customer" });

app.MapControllerRoute(
    name: "product-slug",
    pattern: "products/{slug}",
    defaults: new { controller = "Products", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply the shared database migrations, then seed without deleting/recreating data.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MarketLinkDbContext>();
    await context.Database.MigrateAsync();
    DbInitializer.Seed(context);
}

app.Run();
