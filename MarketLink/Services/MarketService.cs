using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public class MarketService : IMarketService
    {
        private readonly MarketLinkDbContext _context;

        public MarketService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<MarketIndexViewModel> GetMarketsAsync(MarketFilterViewModel filter)
        {
            var query = _context.Markets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(m =>
                    m.MarketName.ToLower().Contains(term) ||
                    m.Address.ToLower().Contains(term));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(m => m.Status == filter.Status.Value);
            }

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;

            var items = await query
                .OrderBy(m => m.MarketName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new MarketIndexViewModel
            {
                Filter = filter,
                Result = new PagedResult<Market>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                TotalMarkets = await _context.Markets.CountAsync(),
                ActiveCount = await _context.Markets.CountAsync(m => m.Status == MarketStatus.Active),
                InactiveCount = await _context.Markets.CountAsync(m => m.Status == MarketStatus.Inactive)
            };

            return vm;
        }

        public async Task<MarketDetailsViewModel?> GetMarketDetailsAsync(int marketId)
        {
            var market = await _context.Markets.FirstOrDefaultAsync(m => m.MarketId == marketId);
            if (market == null)
            {
                return null;
            }

            var farmers = await _context.Farmers
                .Where(f => f.MarketId == marketId)
                .OrderBy(f => f.StallName)
                .ToListAsync();

            var farmerIds = farmers.Select(f => f.FarmerId).ToList();
            var totalProducts = await _context.Products.CountAsync(p => farmerIds.Contains(p.FarmerId));

            return new MarketDetailsViewModel
            {
                Market = market,
                Farmers = farmers,
                TotalFarmers = farmers.Count,
                TotalProducts = totalProducts
            };
        }

        public async Task<MarketEditViewModel?> GetMarketForEditAsync(int marketId)
        {
            var market = await _context.Markets.FindAsync(marketId);
            if (market == null)
            {
                return null;
            }

            return new MarketEditViewModel
            {
                MarketId = market.MarketId,
                MarketName = market.MarketName,
                Address = market.Address,
                OperatingDays = market.OperatingDays,
                OpeningTime = market.OpeningTime,
                ClosingTime = market.ClosingTime,
                Latitude = market.Latitude,
                Longitude = market.Longitude,
                MapProvider = market.MapProvider,
                MapLink = market.MapLink,
                Status = market.Status
            };
        }

        public async Task<int> CreateMarketAsync(MarketCreateViewModel model)
        {
            var market = new Market
            {
                MarketName = model.MarketName,
                Address = model.Address,
                OperatingDays = model.OperatingDays,
                OpeningTime = model.OpeningTime,
                ClosingTime = model.ClosingTime,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                MapProvider = model.MapProvider,
                MapLink = model.MapLink,
                Status = model.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Markets.Add(market);
            await _context.SaveChangesAsync();
            return market.MarketId;
        }

        public async Task<bool> UpdateMarketAsync(MarketEditViewModel model)
        {
            var market = await _context.Markets.FindAsync(model.MarketId);
            if (market == null)
            {
                return false;
            }

            market.MarketName = model.MarketName;
            market.Address = model.Address;
            market.OperatingDays = model.OperatingDays;
            market.OpeningTime = model.OpeningTime;
            market.ClosingTime = model.ClosingTime;
            market.Latitude = model.Latitude;
            market.Longitude = model.Longitude;
            market.MapProvider = model.MapProvider;
            market.MapLink = model.MapLink;
            market.Status = model.Status;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int marketId, MarketStatus newStatus)
        {
            var market = await _context.Markets.FindAsync(marketId);
            if (market == null)
            {
                return false;
            }

            market.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool success, int farmerCount)> DeleteMarketAsync(int marketId)
        {
            var market = await _context.Markets.FindAsync(marketId);
            if (market == null)
            {
                return (false, 0);
            }

            var farmerCount = await _context.Farmers.CountAsync(f => f.MarketId == marketId);

            // Farmers.MarketId is configured with ON DELETE SET NULL, so removing
            // a market simply unlinks any farmers assigned to it rather than
            // failing or cascading — safe to delete regardless of farmerCount.
            _context.Markets.Remove(market);
            await _context.SaveChangesAsync();
            return (true, farmerCount);
        }
    }
}
