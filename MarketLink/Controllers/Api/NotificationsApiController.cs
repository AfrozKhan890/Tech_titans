using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketLink.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/notifications")]
[Produces("application/json")]
public class NotificationsApiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationsApiController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notifs = await _unitOfWork.Repository<Notification>().Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Link,
                n.IsRead,
                n.Type,
                n.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, unreadCount = notifs.Count(n => !n.IsRead), notifications = notifs });
    }

    [HttpPost("mark-read/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notif = await _unitOfWork.Repository<Notification>().GetByIdAsync(id);
        if (notif == null) return NotFound(new { success = false, message = "Notification not found." });
        if (notif.UserId != userId && !User.IsInRole("Admin")) return Forbid();

        notif.IsRead = true;
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { success = true, message = "Marked as read." });
    }
}
