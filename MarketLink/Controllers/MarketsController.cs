using MarketLink.Data;
using MarketLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    public class MarketsController : Controller
    {
        private readonly MarketLinkDbContext _context;
        public MarketsController(MarketLinkDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var markets = await _context.Markets
                .Where(m => m.Status == MarketStatus.Active)
                .OrderBy(m => m.MarketName)
                .ToListAsync();
            return View(markets);
        }

        public async Task<IActionResult> Details(int id)
        {
            var market = await _context.Markets.Include(m => m.Farmers)
                .FirstOrDefaultAsync(m => m.MarketId == id && m.Status == MarketStatus.Active);
            if (market == null) return NotFound();
            return View(market);
        }
    }
}
