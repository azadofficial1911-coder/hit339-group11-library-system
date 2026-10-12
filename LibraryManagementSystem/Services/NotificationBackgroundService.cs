using Microsoft.Extensions.Options;

namespace LibraryManagementSystem.Services
{
    // Runs while the app runs: expires old holds and creates due-soon / overdue reminders.
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly NotificationSettings _settings;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<NotificationSettings> options,
            ILogger<NotificationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TimeSpan interval = TimeSpan.FromMinutes(Math.Max(1, _settings.CheckIntervalMinutes));

            try
            {
                // Give the app and database a moment to start.
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await RunOnceAsync(stoppingToken);
                    await Task.Delay(interval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // The app is shutting down.
            }
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();

                IReservationService reservations =
                    scope.ServiceProvider.GetRequiredService<IReservationService>();

                INotificationService notifications =
                    scope.ServiceProvider.GetRequiredService<INotificationService>();

                await reservations.ExpireOverdueHoldsAsync(ct);
                await notifications.RunScheduledChecksAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Keep running; try again at the next interval.
                _logger.LogError(ex, "The scheduled notification check failed.");
            }
        }
    }
}
