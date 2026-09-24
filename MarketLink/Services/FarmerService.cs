using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public class FarmerService : IFarmerService
    {
        private readonly MarketLinkDbContext _context;

        public FarmerService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<FarmerIndexViewModel> GetFarmersAsync(FarmerFilterViewModel filter)
        {
            var query = _context.Farmers
                .Include(f => f.Market)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(f =>
                    f.StallName.ToLower().Contains(term) ||
                    f.ContactPerson.ToLower().Contains(term) ||
                    f.Email.ToLower().Contains(term) ||
                    f.Phone.Contains(term));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(f => f.Status == filter.Status.Value);
            }

            if (filter.MarketId.HasValue)
            {
                query = query.Where(f => f.MarketId == filter.MarketId.Value);
            }

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;

            var items = await query
                .OrderByDescending(f => f.RegisteredAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new FarmerIndexViewModel
            {
                Filter = filter,
                Result = new PagedResult<Farmer>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                Markets = await _context.Markets.OrderBy(m => m.MarketName).ToListAsync(),
                TotalFarmers = await _context.Farmers.CountAsync(),
                PendingCount = await _context.Farmers.CountAsync(f => f.Status == FarmerStatus.Pending),
                ApprovedCount = await _context.Farmers.CountAsync(f => f.Status == FarmerStatus.Approved),
                SuspendedCount = await _context.Farmers.CountAsync(f => f.Status == FarmerStatus.Suspended)
            };

            return vm;
        }

        public async Task<FarmerDetailsViewModel?> GetFarmerDetailsAsync(int farmerId)
        {
            var farmer = await _context.Farmers
                .Include(f => f.Market)
                .FirstOrDefaultAsync(f => f.FarmerId == farmerId);

            if (farmer == null)
            {
                return null;
            }

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.FarmerId == farmerId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.FarmerId == farmerId)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Product)
                .Where(r => r.FarmerId == farmerId && !r.IsRemoved)
                .OrderByDescending(r => r.ReviewDate)
                .Take(10)
                .ToListAsync();

            var allOrders = await _context.Orders
                .Where(o => o.FarmerId == farmerId && o.OrderStatus != OrderStatus.Cancelled)
                .ToListAsync();

            var allRatings = await _context.Reviews
                .Where(r => r.FarmerId == farmerId && !r.IsRemoved)
                .Select(r => r.Rating)
                .ToListAsync();

            return new FarmerDetailsViewModel
            {
                Farmer = farmer,
                Products = products,
                RecentOrders = recentOrders,
                Reviews = reviews,
                TotalOrders = allOrders.Count,
                TotalRevenue = allOrders.Sum(o => o.TotalAmount),
                AverageRating = allRatings.Any() ? allRatings.Average() : 0
            };
        }

        public async Task<FarmerEditViewModel?> GetFarmerForEditAsync(int farmerId)
        {
            var farmer = await _context.Farmers.FindAsync(farmerId);
            if (farmer == null)
            {
                return null;
            }

            return new FarmerEditViewModel
            {
                FarmerId = farmer.FarmerId,
                StallName = farmer.StallName,
                ContactPerson = farmer.ContactPerson,
                Phone = farmer.Phone,
                Email = farmer.Email,
                Address = farmer.Address,
                OperatingDays = farmer.OperatingDays,
                MarketId = farmer.MarketId,
                PickupWindowStart = farmer.PickupWindowStart,
                PickupWindowEnd = farmer.PickupWindowEnd,
                Status = farmer.Status,
                Markets = await _context.Markets.OrderBy(m => m.MarketName).ToListAsync()
            };
        }

        public async Task<bool> UpdateFarmerAsync(FarmerEditViewModel model)
        {
            var farmer = await _context.Farmers.FindAsync(model.FarmerId);
            if (farmer == null)
            {
                return false;
            }

            farmer.StallName = model.StallName;
            farmer.ContactPerson = model.ContactPerson;
            farmer.Phone = model.Phone;
            farmer.Email = model.Email;
            farmer.Address = model.Address;
            farmer.OperatingDays = model.OperatingDays;
            farmer.MarketId = model.MarketId;
            farmer.PickupWindowStart = model.PickupWindowStart;
            farmer.PickupWindowEnd = model.PickupWindowEnd;
            farmer.Status = model.Status;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int farmerId, FarmerStatus newStatus)
        {
            var farmer = await _context.Farmers.FindAsync(farmerId);
            if (farmer == null)
            {
                return false;
            }

            farmer.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Market>> GetMarketsAsync()
        {
            return await _context.Markets.OrderBy(m => m.MarketName).ToListAsync();
        }
    }
}
