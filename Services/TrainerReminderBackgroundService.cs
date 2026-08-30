using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    // Runs hourly, emailing trainer + student a reminder for any TrainerSession
    // happening in the next 24 hours that hasn't been reminded about yet (PB-10
    // "Date Alerts"). Registered as a singleton hosted service, so it resolves its
    // own scope each pass to reach the scoped DbContext/IEmailSender.
    public class TrainerReminderBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<TrainerReminderBackgroundService> logger;

        public TrainerReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TrainerReminderBackgroundService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Trainer reminder pass failed.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Normal on shutdown.
                }
            }
        }

        private async Task SendDueRemindersAsync(CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var now = DateTime.UtcNow;
            var windowEnd = now.AddHours(24);

            var due = await db.TrainerSessions
                .Include(s => s.Trainer)
                .Include(s => s.Student)
                .Where(s => !s.ReminderSent && s.ScheduledAt >= now && s.ScheduledAt <= windowEnd)
                .ToListAsync(ct);

            foreach (var session in due)
            {
                var when = session.ScheduledAt.ToString("f");

                await emailSender.SendAsync(session.Student!.Email, "Upcoming PT session reminder",
                    $"Reminder: you have a personal trainer session with {session.Trainer!.EmpName} on {when}.");

                await emailSender.SendAsync(session.Trainer!.Email, "Upcoming PT session reminder",
                    $"Reminder: you have a personal trainer session with {session.Student!.EmpName} on {when}.");

                session.ReminderSent = true;
            }

            if (due.Count > 0)
            {
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
