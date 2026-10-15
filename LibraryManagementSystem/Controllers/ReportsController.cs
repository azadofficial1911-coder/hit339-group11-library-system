using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Manager)]
    public class ReportsController : Controller
    {
        private readonly ReportService _reportService;

        public ReportsController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate,
            string? itemType,
            CancellationToken cancellationToken)
        {
            var report = await _reportService.GenerateAsync(
                fromDate, toDate, itemType, cancellationToken);

            if (!ModelState.IsValid)
            {
                report.Errors.Add(
                    "The filter contains an invalid date or value.");
            }

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            DateTime? fromDate,
            DateTime? toDate,
            string? itemType,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("The report filters are invalid.");
            }

            var report = await _reportService.GenerateAsync(
                fromDate, toDate, itemType, cancellationToken);

            if (report.Errors.Count > 0)
            {
                return View("Index", report);
            }

            return File(
                ReportCsvExporter.Export(report),
                "text/csv; charset=utf-8",
                $"library-report-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(
            DateTime? fromDate,
            DateTime? toDate,
            string? itemType,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("The report filters are invalid.");
            }

            var report = await _reportService.GenerateAsync(
                fromDate, toDate, itemType, cancellationToken);

            if (report.Errors.Count > 0)
            {
                return View("Index", report);
            }

            return File(
                ReportPdfExporter.Export(report),
                "application/pdf",
                $"library-report-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        }
    }
}