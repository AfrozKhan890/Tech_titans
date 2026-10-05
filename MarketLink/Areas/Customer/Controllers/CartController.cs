using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class CartController : MarketLink.Services.CustomerAreaController
{
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;

    public CartController(ICartService cartService, IUnitOfWork unitOfWork)
    {
        _cartService = cartService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Shopping Cart";
        var items = (await _cartService.GetCartAsync(CustomerId)).ToList();

        var farmerIds = items.Where(i => i.Product != null).Select(i => i.Product.FarmerId).Distinct().ToList();
        var slotsByFarmer = (await _unitOfWork.Repository<MarketLink.Models.PickupSlot>().Query()
                .Include(s => s.Market)
                .Where(s => farmerIds.Contains(s.FarmerId) && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync())
            .GroupBy(s => s.FarmerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var model = new MarketLink.Models.CartViewModel
        {
            Groups = items
                .Where(i => i.Product != null)
                .GroupBy(i => i.Product.FarmerId)
                .Select(g => new MarketLink.Models.CartFarmGroup
                {
                    FarmerId = g.Key,
                    FarmName = g.First().Product.Farmer?.FarmName ?? "Local Farm",
                    Items = g.ToList(),
                    Slots = slotsByFarmer.TryGetValue(g.Key, out var list) ? list : []
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest req)
    {
        if (req == null || req.ProductId <= 0 || req.QuantityKg <= 0)
            return Json(new { success = false, message = "Invalid request." });

        try
        {
            var customerId = CustomerId;
            await _cartService.AddToCartAsync(customerId, req.ProductId, req.QuantityKg);
            var summary = await _cartService.GetCartSummaryAsync(customerId);
            return Json(new { success = true, cartCount = summary.ItemCount });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest req)
    {
        if (req == null || req.CartItemId <= 0)
            return Json(new { success = false, message = "Invalid request." });

        try
        {
            await _cartService.UpdateCartItemAsync(req.CartItemId, req.QuantityKg, CustomerId);
            return Json(new { success = true, total = await _cartService.GetCartTotalAsync(CustomerId) });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveFromCartRequest req)
    {
        if (req == null)
            return Json(new { success = false, message = "Invalid request." });

        try
        {
            var cartItemId = req.CartItemId;
            if (cartItemId <= 0)
            {
                var cart = await _cartService.GetCartAsync(CustomerId);
                cartItemId = cart.FirstOrDefault(c => c.ProductId == req.ProductId)?.Id ?? 0;
            }

            if (cartItemId > 0) await _cartService.RemoveFromCartAsync(cartItemId, CustomerId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var customerId = CustomerId;
        await _cartService.ClearCartAsync(customerId);
        return RedirectToAction(nameof(Index));
    }
}

public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int QuantityKg { get; set; }
}

public class UpdateCartItemRequest
{
    public int CartItemId { get; set; }
    public int QuantityKg { get; set; }
}

public class RemoveFromCartRequest
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
}
