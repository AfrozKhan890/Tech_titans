using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Models;
using MarketLink.Repositories;
using MarketLink.Services;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Farmer.Controllers;

[Area("Farmer")]
[Authorize(Roles = "Farmer")]
public class ProductsController : FarmerAreaController
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

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "My Produce Catalog";
        var products = await _unitOfWork.Repository<Product>().Query()
            .Include(p => p.Category)
            .Where(p => p.FarmerId == FarmerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Add New Produce";
        await LoadCategoriesAsync();
        return View(new ProductInputModel { Unit = "KG", StockQuantityKg = 50, PricePerKg = 3.50m, IsAvailable = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductInputModel model)
    {
        if (!await PrepareAsync(model)) return View(model);

        var (uploaded, error) = await _imageStore.SaveAsync(model.ImageFile);
        if (error != null)
        {
            ModelState.AddModelError(nameof(ProductInputModel.ImageFile), error);
            return View(model);
        }

        var product = new Product
        {
            FarmerId = FarmerId,
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
            ImageUrl = uploaded ?? (string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim())
        };

        await _productService.CreateAsync(product, string.IsNullOrEmpty(product.ImageUrl) ? [] : [product.ImageUrl]);
        TempData["Success"] = $"Produce '{product.Name}' added successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await FindOwnedAsync(id);
        if (product == null) return NotFound();

        ViewData["Title"] = $"Edit {product.Name}";
        await LoadCategoriesAsync();

        return View(new ProductInputModel
        {
            Id = product.Id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            PricePerKg = product.PricePerKg,
            StockQuantityKg = product.StockQuantityKg,
            Unit = product.Unit,
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            Tags = product.Tags,
            Season = product.Season,
            IsOrganic = product.IsOrganic,
            IsAvailable = product.IsAvailable,
            FarmerId = product.FarmerId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductInputModel model)
    {
        var existing = await FindOwnedAsync(model.Id);
        if (existing == null) return NotFound();

        ViewData["Title"] = $"Edit {model.Name}";
        if (!await PrepareAsync(model)) return View(model);

        var (uploaded, error) = await _imageStore.SaveAsync(model.ImageFile);
        if (error != null)
        {
            ModelState.AddModelError(nameof(ProductInputModel.ImageFile), error);
            return View(model);
        }

        existing.Name = model.Name.Trim();
        existing.CategoryId = model.CategoryId;
        existing.Description = model.Description?.Trim();
        existing.PricePerKg = model.PricePerKg;
        existing.StockQuantityKg = model.StockQuantityKg;
        existing.Unit = model.Unit.Trim();
        existing.Tags = model.Tags?.Trim();
        existing.Season = model.Season;
        existing.IsOrganic = model.IsOrganic;
        existing.IsAvailable = model.IsAvailable;
        var typedUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();
        var newImageUrl = uploaded ?? typedUrl ?? existing.ImageUrl;
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

        TempData["Success"] = "Produce updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await FindOwnedAsync(id);
        if (product == null) return NotFound();

        var wasAvailable = product.IsAvailable;
        await _productService.DeleteAsync(product.Id);
        TempData["Success"] = wasAvailable
            ? "Produce deleted."
            : "Produce unlisted — it still appears in past orders.";
        return RedirectToAction(nameof(Index));
    }

    private Task<Product?> FindOwnedAsync(int id)
        => _unitOfWork.Repository<Product>().Query()
            .FirstOrDefaultAsync(p => p.Id == id && p.FarmerId == FarmerId);

    private async Task LoadCategoriesAsync()
    {
        var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
        ViewBag.Categories = categories.OrderBy(c => c.Name).ToList();
    }

    private async Task<bool> PrepareAsync(ProductInputModel model)
    {
        if (model.CategoryId <= 0 || !await _unitOfWork.Repository<Category>().AnyAsync(c => c.Id == model.CategoryId))
            ModelState.AddModelError(nameof(ProductInputModel.CategoryId), "Please choose a valid category.");

        if (!ModelState.IsValid) await LoadCategoriesAsync();
        return ModelState.IsValid;
    }
}
