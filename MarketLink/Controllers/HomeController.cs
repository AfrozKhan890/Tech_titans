using MarketLink.Data;
using MarketLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    public class HomeController : Controller
    {
        private readonly MarketLinkDbContext _context;
        public HomeController(MarketLinkDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Farmer)
                .Where(p => p.Status == ProductStatus.Active && p.Farmer != null && p.Farmer.Status == FarmerStatus.Approved)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
            var markets = await _context.Markets.Where(m => m.Status == MarketStatus.Active).OrderBy(m => m.MarketName).ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Markets = markets;
            return View(products);
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();
        public IActionResult FAQ() => View();
        public IActionResult Privacy() => View();
        public IActionResult Terms() => View();
        public IActionResult Error() => View();
    }
}
