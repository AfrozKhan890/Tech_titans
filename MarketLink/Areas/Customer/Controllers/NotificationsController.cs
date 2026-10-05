using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class NotificationsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Notifications";
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth", new { area = "" });

        var notifications = await _unitOfWork.Repository<Notification>().Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notif = await _unitOfWork.Repository<Notification>().GetByIdAsync(id);
        if (notif != null && notif.UserId == userId)
        {
            notif.IsRead = true;
            await _unitOfWork.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var unread = await _unitOfWork.Repository<Notification>().Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
        }
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Json(new { count = 0 });

        var count = await _unitOfWork.Repository<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead);
        return Json(new { count });
    }
}
