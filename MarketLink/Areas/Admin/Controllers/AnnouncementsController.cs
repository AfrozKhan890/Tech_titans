using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Models;
using MarketLink.Services;
using MarketLink.Areas.Admin.Services;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

using MarketLink.Areas.Admin;
namespace MarketLink.Areas.Admin.Controllers
{
    [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
    [Area("Admin")]
    public class AnnouncementsController : Controller
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementsController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        private int CurrentAdminId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // GET: /Announcements
        public async Task<IActionResult> Index(AnnouncementFilterViewModel filter)
        {
            var vm = await _announcementService.GetAnnouncementsAsync(filter);
            return View(vm);
        }

        // GET: /Announcements/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _announcementService.GetDetailsAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // GET: /Announcements/Create
        public IActionResult Create()
        {
            return View(new AnnouncementCreateViewModel());
        }

        // POST: /Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnouncementCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var id = await _announcementService.CreateAsync(model, CurrentAdminId);
            TempData["SuccessMessage"] = "Announcement created.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Announcements/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _announcementService.GetForEditAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // POST: /Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AnnouncementEditViewModel model)
        {
            if (id != model.AnnouncementId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ok = await _announcementService.UpdateAsync(model);
            if (!ok)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Announcement updated.";
            return RedirectToAction(nameof(Details), new { id = model.AnnouncementId });
        }

        // POST: /Announcements/Publish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id, string? returnUrl = null)
        {
            await _announcementService.ChangeStatusAsync(id, AnnouncementStatus.Published);
            TempData["SuccessMessage"] = "Announcement published.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Announcements/Unpublish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(int id, string? returnUrl = null)
        {
            await _announcementService.ChangeStatusAsync(id, AnnouncementStatus.Unpublished);
            TempData["SuccessMessage"] = "Announcement unpublished.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Announcements/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _announcementService.DeleteAsync(id);
            if (!ok)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Announcement deleted.";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult RedirectBack(string? returnUrl, int id)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}