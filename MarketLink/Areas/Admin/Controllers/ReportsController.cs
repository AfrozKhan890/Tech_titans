using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Services;
using MarketLink.Areas.Admin.Services;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

using MarketLink.Areas.Admin;
namespace MarketLink.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin Reports page. All figures come from the database via
    /// ReportService and honour the posted filters.
    /// </summary>
    [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: /Reports
        public async Task<IActionResult> Index(ReportFilterViewModel filter)
        {
            var vm = await _reportService.GetReportAsync(filter);
            return View(vm);
        }
    }
}