using MarketLink.Models;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Services.Ai;
using MarketLink.Data;
using MarketLink.Data.SeedData;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<ApplicationUserClaimsFactory>();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);





    options.Events.OnValidatePrincipal = async context =>
    {
        var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.RejectPrincipal();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsActive)
        {
            context.RejectPrincipal();
            return;
        }

        var roleManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = await roleManager.GetRolesAsync(user);

        if (roles.Contains("Farmer", StringComparer.OrdinalIgnoreCase))
        {
            var farmer = await db.Farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (farmer == null || farmer.Status == MarketLink.Models.Enums.FarmerStatus.Suspended)
            {
                context.RejectPrincipal();
                return;
            }
        }
    };
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("FarmerOnly", policy => policy.RequireRole("Farmer"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
    options.AddPolicy("FarmerOrAdmin", policy => policy.RequireRole("Farmer", "Admin"));
});


builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IFarmerService, FarmerService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ProductImageStore>();







builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});


builder.Services.Configure<AiSettings>(builder.Configuration.GetSection("Ai"));
builder.Services.AddHttpClient<IAiAssistantService, GeminiAiAssistantService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
});
builder.Services.AddScoped<AiContextBuilder>();


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MarketLink REST API",
        Version = "v1",
        Description = "Production-grade REST APIs for MarketLink Farmers Marketplace platform (Auth, Products, Categories, Markets, Orders, Reviews, Favorites, Notifications)."
    });
});
builder.Services.AddHttpContextAccessor();


builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

var app = builder.Build();


if (!HasFreePort(app))
{
    Environment.ExitCode = 1;
    return;
}


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DatabaseSeeder.SeedAsync(db, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the database.");
    }
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketLink REST API v1");
    c.RoutePrefix = "swagger";
});


app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), branch =>
    branch.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}"));

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "product-slug",
    pattern: "products/{slug:regex(^(?!index$|details$|autocomplete$).+$)}",
    defaults: new { controller = "Products", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

try
{
    app.Run();
}
catch (IOException ex) when (ex.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase))
{

    Console.WriteLine("MarketLink could not start: another program already holds this port.");
    Console.WriteLine("  Stop the other instance, then run this command again.");
    Environment.ExitCode = 1;
}



bool HasFreePort(WebApplication application)
{


    string urls = application.Configuration["urls"]
        ?? application.Configuration["ASPNETCORE_URLS"]
        ?? "http://localhost:5000";

    foreach (string url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? address)) continue;

        int port = address.IsDefaultPort
            ? address.Scheme == Uri.UriSchemeHttps ? 443 : 80
            : address.Port;

        IPAddress[] hosts;
        try
        {
            hosts = address.HostNameType == UriHostNameType.Dns
                ? Dns.GetHostAddresses(address.Host)
                : [IPAddress.Parse(address.Host)];
        }
        catch (SocketException)
        {
            continue;
        }

        foreach (IPAddress host in hosts)
        {
            using var probe = new TcpClient();
            bool connected;
            try
            {
                connected = probe.ConnectAsync(host, port).Wait(TimeSpan.FromMilliseconds(250));
            }
            catch (Exception)
            {

                connected = false;
            }

            if (!connected) continue;

            Console.WriteLine($"MarketLink could not start: {url} is already in use.");
            Console.WriteLine("  Stop the other instance, or run this one on a spare port:");
            Console.WriteLine("      dotnet run --urls http://localhost:5231");
            return false;
        }
    }

    return true;
}


public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
