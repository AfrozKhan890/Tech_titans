using Microsoft.AspNetCore.Mvc;
using MarketLink.Models;
using MarketLink.Models.Enums;
using MarketLink.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers;

public class MarketsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public MarketsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IActionResult> Index(string? search, string? city, string? day, double? latitude, double? longitude, double? radiusKm)
    {
        var query = _unitOfWork.Repository<Market>().Query()
            .Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m => m.Name.Contains(term) || m.Address.Contains(term) || m.City.Contains(term) || m.State.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var location = city.Trim();
            query = query.Where(m => m.City.Contains(location) || m.State.Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(day))
        {
            var dayName = ParseDay(day);
            if (dayName.HasValue)
            {
                var token = dayName.Value.ToString();
                query = query.Where(m => m.OpenDays != null && m.OpenDays.Contains(token));
            }
        }

        var markets = await query.OrderBy(m => m.Name).ToListAsync();

        if (latitude.HasValue && longitude.HasValue)
        {
            var maxRadius = radiusKm.GetValueOrDefault(50);
            markets = markets
                .Select(m => new { Market = m, Distance = DistanceKm(latitude.Value, longitude.Value, m.Latitude, m.Longitude) })
                .Where(x => x.Distance <= maxRadius)
                .OrderBy(x => x.Distance)
                .Select(x => x.Market)
                .ToList();
            ViewBag.Nearby = true;
        }

        ViewBag.Search = search;
        ViewBag.City = city;
        ViewBag.Day = day;
        ViewBag.Latitude = latitude;
        ViewBag.Longitude = longitude;
        ViewBag.RadiusKm = radiusKm.GetValueOrDefault(50);
        return View(markets);
    }

    public async Task<IActionResult> Details(int id)
    {
        var market = await _unitOfWork.Repository<Market>()
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
        if (market == null) return NotFound();

        var stalls = await _unitOfWork.Repository<FarmerMarket>()
            .Query()
            .Include(fm => fm.Farmer)
            .Where(fm => fm.MarketId == id && fm.IsActive && fm.Market.IsActive && fm.Farmer.Status == FarmerStatus.Approved)
            .OrderBy(fm => fm.Farmer.FarmName)
            .ToListAsync();

        var stallIds = stalls.Select(s => s.FarmerId).ToList();
        List<Product> products = [];
        List<PickupSlot> slots = [];
        if (stallIds.Count > 0)
        {
            products = await _unitOfWork.Repository<Product>().Query()
                .Include(p => p.Farmer)
                .Where(p => p.IsAvailable && p.StockQuantityKg > 0 && stallIds.Contains(p.FarmerId))
                .OrderBy(p => p.Name)
                .Take(12)
                .ToListAsync();

            slots = await _unitOfWork.Repository<PickupSlot>().Query()
                .Include(s => s.Farmer)
                .Where(s => s.MarketId == id && s.IsActive && s.Farmer.Status == FarmerStatus.Approved)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        ViewBag.Stalls = stalls;
        ViewBag.Products = products;
        ViewBag.Slots = slots;
        return View(market);
    }

    private static DayOfWeek? ParseDay(string value)
    {
        return Enum.TryParse<DayOfWeek>(value, true, out var day) ? day : null;
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0088;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusKm * 2 * Math.Asin(Math.Sqrt(a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
