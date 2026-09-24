using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly MarketLinkDbContext _context;

        public CategoryService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<CategoryIndexViewModel> GetCategoriesAsync(CategoryFilterViewModel filter)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term)));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == filter.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;

            var items = await query
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new CategoryIndexViewModel
            {
                Filter = filter,
                Result = new PagedResult<Category>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                TotalCategories = await _context.Categories.CountAsync(),
                ActiveCount = await _context.Categories.CountAsync(c => c.IsActive),
                InactiveCount = await _context.Categories.CountAsync(c => !c.IsActive)
            };
        }

        public async Task<CategoryEditViewModel?> GetForEditAsync(int categoryId)
        {
            var c = await _context.Categories.FirstOrDefaultAsync(x => x.CategoryId == categoryId);
            if (c == null)
            {
                return null;
            }

            var productCount = await _context.Products.CountAsync(p => p.CategoryId == categoryId);

            return new CategoryEditViewModel
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                ProductCount = productCount
            };
        }

        public async Task<int> CreateAsync(CategoryCreateViewModel model)
        {
            var category = new Category
            {
                Name = model.Name.Trim(),
                Slug = Slugify(model.Name),
                Description = model.Description?.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category.CategoryId;
        }

        public async Task<bool> UpdateAsync(CategoryEditViewModel model)
        {
            var category = await _context.Categories.FindAsync(model.CategoryId);
            if (category == null)
            {
                return false;
            }

            category.Name = model.Name.Trim();
            category.Slug = Slugify(model.Name);
            category.Description = model.Description?.Trim();
            category.IsActive = model.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int categoryId, bool isActive)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return false;
            }

            category.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }

        private static string Slugify(string value) =>
            Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

        public async Task<(bool success, string? error)> DeleteAsync(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return (false, "Category not found.");
            }

            // Products.CategoryId is configured with ON DELETE SET NULL,
            // so removing the category simply unlinks products rather than
            // failing or cascading. Keep it safe regardless.
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}