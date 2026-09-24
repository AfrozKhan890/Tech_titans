using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    public interface IReportService
    {
        Task<ReportsIndexViewModel> GetReportAsync(ReportFilterViewModel filter);
    }
}