using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    // Simulated email / SMS log (R3). Patrons never see this page.
    [Authorize(Roles = RoleNames.Reception + "," + RoleNames.Manager)]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public NotificationsController(
            ApplicationDbContext context,
            INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        public async Task<IActionResult> Index(
            string? searchTerm,
            string? channel,
            string? eventType)
        {
            IQueryable<NotificationLog> query = _context.NotificationLogs
                .AsNoTracking()
                .Include(notification => notification.Borrower);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(notification =>
                    notification.Borrower.BorrowerCode.Contains(searchTerm) ||
                    notification.Borrower.FullName.Contains(searchTerm) ||
                    notification.Recipient.Contains(searchTerm) ||
                    notification.Subject.Contains(searchTerm));
            }

            if (Enum.TryParse(channel, true, out NotificationChannel selectedChannel))
            {
                query = query.Where(notification => notification.Channel == selectedChannel);
            }

            if (Enum.TryParse(eventType, true, out NotificationEventType selectedEvent))
            {
                query = query.Where(notification => notification.EventType == selectedEvent);
            }

            int total = await query.CountAsync();

            List<NotificationLog> rows = await query
                .OrderByDescending(notification => notification.CreatedAt)
                .ThenByDescending(notification => notification.Id)
                .Take(200)
                .ToListAsync();

            return View(new NotificationsIndexViewModel
            {
                Notifications = rows,
                SearchTerm = searchTerm ?? string.Empty,
                SelectedChannel = channel ?? string.Empty,
                SelectedEvent = eventType ?? string.Empty,
                TotalMatching = total
            });
        }

        // Runs the same checks as the background job, for demos and testing.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunChecks()
        {
            int created = await _notifications.RunScheduledChecksAsync();

            TempData["SuccessMessage"] = created == 0
                ? "Checks finished. No new reminders were needed."
                : $"Checks finished. {created} new message(s) were logged.";

            return RedirectToAction(nameof(Index));
        }
    }
}
