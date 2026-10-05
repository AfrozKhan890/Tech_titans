using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoriesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Category Management";
        var categories = await _unitOfWork.Repository<Category>().Query()
            .Include(c => c.Products)
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description, string? iconClass, int displayOrder = 0, int? parentId = null)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            TempData["Error"] = "Category name cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        if (parentId.HasValue && !await _unitOfWork.Repository<Category>().AnyAsync(c => c.Id == parentId.Value))
        {
            TempData["Error"] = "The selected parent category does not exist.";
            return RedirectToAction(nameof(Index));
        }

        var slug = await BuildUniqueSlugAsync(name);
        var cat = new Category
        {
            Name = name,
            Slug = slug,
            Description = description?.Trim(),
            IconClass = iconClass?.Trim() ?? "fas fa-leaf",
            SortOrder = displayOrder,
            ParentId = parentId,
            IsActive = true
        };

        await _unitOfWork.Repository<Category>().AddAsync(cat);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Category '{name}' created!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name, string? description, string? iconClass, int displayOrder, bool isActive, int? parentId = null)
    {
        var cat = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
        if (cat == null) return RedirectToAction(nameof(Index));

        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            TempData["Error"] = "Category name cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        if (parentId == id)
        {
            TempData["Error"] = "A category cannot be its own parent.";
            return RedirectToAction(nameof(Index));
        }

        if (parentId.HasValue && !await IsValidParentAsync(id, parentId.Value))
        {
            TempData["Error"] = "The selected parent would create an invalid category cycle.";
            return RedirectToAction(nameof(Index));
        }

        cat.Name = name;
        cat.Description = description?.Trim();
        cat.IconClass = iconClass?.Trim() ?? "fas fa-leaf";
        cat.SortOrder = displayOrder;
        cat.IsActive = isActive;
        cat.ParentId = parentId;


        if (string.IsNullOrWhiteSpace(cat.Slug))
            cat.Slug = await BuildUniqueSlugAsync(name, id);

        await _unitOfWork.Repository<Category>().UpdateAsync(cat);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Category updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _unitOfWork.Repository<Category>().Query()
            .Include(c => c.Products)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cat != null)
        {
            if (cat.Children.Any())
            {
                TempData["Error"] = $"Cannot delete category '{cat.Name}' because it has child categories. Move or delete them first.";
            }
            else if (cat.Products.Any())
            {
                TempData["Error"] = $"Cannot delete category '{cat.Name}' because it contains {cat.Products.Count} products.";
            }
            else
            {
                _unitOfWork.Repository<Category>().Remove(cat);
                await _unitOfWork.SaveChangesAsync();
                TempData["Success"] = "Category deleted.";
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<string> BuildUniqueSlugAsync(string name, int? excludeId = null)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;
        var suffix = 2;

        while (await _unitOfWork.Repository<Category>().AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value)))
            slug = $"{baseSlug}-{suffix++}";

        return slug;
    }

    private async Task<bool> IsValidParentAsync(int categoryId, int parentId)
    {
        var categories = await _unitOfWork.Repository<Category>().Query()
            .Select(c => new { c.Id, c.ParentId })
            .ToDictionaryAsync(c => c.Id, c => c.ParentId);

        var current = parentId;
        var visited = new HashSet<int>();
        while (current != 0 && visited.Add(current))
        {
            if (current == categoryId) return false;
            if (!categories.TryGetValue(current, out var next) || !next.HasValue) break;
            current = next.Value;
        }

        return categories.ContainsKey(parentId);
    }

    private static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant()
            .Replace("&", "and")
            .Replace("'", string.Empty)
            .Replace("/", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9]+", "-");
        return slug.Trim('-');
    }
}
