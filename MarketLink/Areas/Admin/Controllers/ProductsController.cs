using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;
using FarmerEntity = MarketLink.Models.Farmer;

namespace MarketLink.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductService _productService;
    private readonly ProductImageStore _imageStore;

    public ProductsController(IUnitOfWork unitOfWork, IProductService productService, ProductImageStore imageStore)
    {
        _unitOfWork = unitOfWork;
        _productService = productService;
        _imageStore = imageStore;
    }

    public async Task<IActionResult> Index(int? categoryId, int? farmerId)
    {
        ViewData["Title"] = "Product Moderation";

        var query = _unitOfWork.Repository<Product>().Query()
            .Include(p => p.Category)
            .Include(p => p.Farmer)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (farmerId.HasValue)
            query = query.Where(p => p.FarmerId == farmerId.Value);

        var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

        var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
        var farmers = await _unitOfWork.Repository<FarmerEntity>().GetAllAsync();

        ViewBag.Categories = categories.OrderBy(c => c.Name).ToList();
        ViewBag.Farmers = farmers.OrderBy(f => f.FarmName).ToList();

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
        if (product != null)
        {
            product.IsAvailable = !product.IsAvailable;
            await _unitOfWork.Repository<Product>().UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Produce '{product.Name}' availability toggled.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
        if (product != null)
        {
            await _productService.DeleteAsync(id);
            TempData["Success"] = $"Produce '{product.Name}' removed from the marketplace.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Add Produce";
        await LoadListsAsync();
        return View("Form", new ProductInputModel { Unit = "KG", StockQuantityKg = 50, PricePerKg = 3.50m, IsAvailable = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductInputModel model)
    {
        if (!await ValidateAsync(model)) return View("Form", model);

        var (uploaded, error) = await _imageStore.SaveAsync(model.ImageFile);
        if (error != null)
        {
            ModelState.AddModelError(nameof(ProductInputModel.ImageFile), error);
            return View("Form", model);
        }

        var product = new Product
        {
            FarmerId = model.FarmerId!.Value,
            CategoryId = model.CategoryId,
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            PricePerKg = model.PricePerKg,
            StockQuantityKg = model.StockQuantityKg,
            Unit = model.Unit.Trim(),
            Tags = model.Tags?.Trim(),
            Season = model.Season,
            IsOrganic = model.IsOrganic,
            IsAvailable = model.IsAvailable,
            AvailableQuantities = "[1,2,5,10,25,50]",
            ImageUrl = uploaded ?? model.ImageUrl?.Trim()
        };

        await _productService.CreateAsync(product, string.IsNullOrEmpty(product.ImageUrl) ? [] : [product.ImageUrl]);
        TempData["Success"] = $"'{product.Name}' is now listed in the marketplace.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _unitOfWork.Repository<Product>().Query().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        ViewData["Title"] = $"Edit {product.Name}";
        await LoadListsAsync();

        return View("Form", new ProductInputModel
        {
            Id = product.Id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            FarmerId = product.FarmerId,
            PricePerKg = product.PricePerKg,
            StockQuantityKg = product.StockQuantityKg,
            Unit = product.Unit,
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            Tags = product.Tags,
            Season = product.Season,
            IsOrganic = product.IsOrganic,
            IsAvailable = product.IsAvailable
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductInputModel model)
    {
        var existing = await _unitOfWork.Repository<Product>().GetByIdAsync(model.Id);
        if (existing == null) return NotFound();

        ViewData["Title"] = $"Edit {model.Name}";
        if (!await ValidateAsync(model)) return View("Form", model);

        var (uploaded, error) = await _imageStore.SaveAsync(model.ImageFile);
        if (error != null)
        {
            ModelState.AddModelError(nameof(ProductInputModel.ImageFile), error);
            return View("Form", model);
        }

        existing.Name = model.Name.Trim();
        existing.CategoryId = model.CategoryId;
        existing.FarmerId = model.FarmerId!.Value;
        existing.Description = model.Description?.Trim();
        existing.PricePerKg = model.PricePerKg;
        existing.StockQuantityKg = model.StockQuantityKg;
        existing.Unit = model.Unit.Trim();
        existing.Tags = model.Tags?.Trim();
        existing.Season = model.Season;
        existing.IsOrganic = model.IsOrganic;
        existing.IsAvailable = model.IsAvailable;
        var newImageUrl = uploaded ?? model.ImageUrl?.Trim() ?? existing.ImageUrl;
        var imageChanged = !string.Equals(existing.ImageUrl, newImageUrl, StringComparison.OrdinalIgnoreCase);
        existing.ImageUrl = newImageUrl;
        existing.UpdatedAt = DateTime.UtcNow;

        if (imageChanged)
            await _productService.UpdateAsync(existing, string.IsNullOrWhiteSpace(newImageUrl) ? [] : [newImageUrl]);
        else
        {
            await _unitOfWork.Repository<Product>().UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();
        }

        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync()
    {
        var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
        var farmers = await _unitOfWork.Repository<FarmerEntity>().GetAllAsync();
        ViewBag.Categories = categories.OrderBy(c => c.Name).ToList();
        ViewBag.Farmers = farmers.OrderBy(f => f.FarmName).ToList();
    }

    private async Task<bool> ValidateAsync(ProductInputModel model)
    {
        if (model.FarmerId is null || model.FarmerId <= 0 || !await _unitOfWork.Repository<FarmerEntity>().AnyAsync(f => f.Id == model.FarmerId))
            ModelState.AddModelError(nameof(ProductInputModel.FarmerId), "Please choose the farm that owns this produce.");

        if (model.CategoryId <= 0 || !await _unitOfWork.Repository<Category>().AnyAsync(c => c.Id == model.CategoryId))
            ModelState.AddModelError(nameof(ProductInputModel.CategoryId), "Please choose a valid category.");

        if (!ModelState.IsValid) await LoadListsAsync();
        return ModelState.IsValid;
    }
}
