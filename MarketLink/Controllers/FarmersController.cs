using MarketLink.Data;
using MarketLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    public class FarmersController : Controller
    {
        private readonly MarketLinkDbContext _context;
        public FarmersController(MarketLinkDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var farmers = await _context.Farmers
                .Where(f => f.Status == FarmerStatus.Approved)
                .OrderByDescending(f => f.IsFeatured)
                .ThenByDescending(f => f.RegisteredAt)
                .ToListAsync();
            return View(farmers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.FarmerId == id && f.Status == FarmerStatus.Approved);
            if (farmer == null) return NotFound();

            ViewBag.Products = await _context.Products
                .Where(p => p.FarmerId == id && p.Status == ProductStatus.Active)
                .Include(p => p.Category)
                .ToListAsync();
            return View(farmer);
        }
    }
}
