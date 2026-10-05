using Microsoft.AspNetCore.Mvc;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers.Api;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesApiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoriesApiController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _unitOfWork.Repository<Category>().Query()
            .Include(c => c.Children)
            .Where(c => c.ParentId == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.IconClass,
                SubCategories = c.Children
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new { s.Id, s.Name, s.Slug })
            })
            .ToListAsync();

        return Ok(new { success = true, categories });
    }
}
