using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    public record BusyHourPrediction(int Hour, int HistoricalCheckIns, CapacityLevel PredictedLevel);

    public record BestTimeToGo(int Hour, string DisplayLabel);

    // Historical/predictive capacity ("peak hours by day", "best time to go
    // today"), built on top of AttendanceReportService's existing grouping
    // methods and GymCapacityService's Light/Moderate/Heavy classification, so
    // predicted and live status read consistently. Calculation logic is pure
    // static methods; instance methods fetch the trailing-window CheckIn data.
    public class PredictiveCapacityService
    {
        private readonly MyDbContext db;
        private readonly IConfiguration configuration;

        public PredictiveCapacityService(MyDbContext db, IConfiguration configuration)
        {
            this.db = db;
            this.configuration = configuration;
        }

        public int PredictionTrailingWeeks =>
            configuration.GetValue<int?>("CapacitySlots:PredictionTrailingWeeks") ?? 8;

        public static List<BusyHourPrediction> RankHoursByHistoricalLoad(
            Dictionary<int, int> checkInCountsByHour, int threshold, double moderateAt, double heavyAt)
        {
            return checkInCountsByHour
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new BusyHourPrediction(
                    kvp.Key, kvp.Value, GymCapacityService.CalculateLevel(kvp.Value, threshold, moderateAt, heavyAt)))
                .ToList();
        }

        // Quietest hour at or after currentHour today; if the day's effectively
        // over, falls back to the quietest hour across the whole day.
        public static BestTimeToGo FindQuietestUpcomingHour(List<BusyHourPrediction> todaysPredictions, int currentHour)
        {
            var upcoming = todaysPredictions.Where(p => p.Hour >= currentHour).ToList();
            var pool = upcoming.Count > 0 ? upcoming : todaysPredictions;

            if (pool.Count == 0)
            {
                return new BestTimeToGo(currentHour, "No historical data yet");
            }

            var quietest = pool.OrderBy(p => p.HistoricalCheckIns).ThenBy(p => p.Hour).First();
            var label = $"{quietest.Hour:D2}:00 - {(quietest.Hour + 1) % 24:D2}:00";
            return new BestTimeToGo(quietest.Hour, label);
        }

        public async Task<List<BusyHourPrediction>> GetTodaysPeakHoursAsync(DateTime? asOfUtc = null)
        {
            var asOf = asOfUtc ?? DateTime.UtcNow;
            var windowStart = asOf.AddDays(-7 * PredictionTrailingWeeks);

            // Pull the trailing window and filter to today's weekday in memory -
            // DayOfWeek isn't something EF Core can translate cleanly to SQL.
            var checkInTimes = await db.CheckIns
                .Where(c => c.CheckInTime >= windowStart && c.CheckInTime <= asOf)
                .Select(c => c.CheckInTime)
                .ToListAsync();
            var sameWeekday = checkInTimes.Where(t => t.DayOfWeek == asOf.DayOfWeek);

            var threshold = configuration.GetValue<int?>("GymCapacity:Threshold") ?? 100;
            var moderateAt = configuration.GetValue<double?>("GymCapacity:ModerateAt") ?? 0.5;
            var heavyAt = configuration.GetValue<double?>("GymCapacity:HeavyAt") ?? 0.8;

            var byHour = AttendanceReportService.GroupByHourOfDay(sameWeekday);
            return RankHoursByHistoricalLoad(byHour, threshold, moderateAt, heavyAt);
        }

        public async Task<BestTimeToGo> GetBestTimeToGoTodayAsync(DateTime? asOfUtc = null)
        {
            var asOf = asOfUtc ?? DateTime.UtcNow;
            var predictions = await GetTodaysPeakHoursAsync(asOf);
            return FindQuietestUpcomingHour(predictions, asOf.Hour);
        }

        public async Task<Dictionary<DayOfWeek, int>> GetPeakDaysAsync(DateTime? asOfUtc = null)
        {
            var asOf = asOfUtc ?? DateTime.UtcNow;
            var windowStart = asOf.AddDays(-7 * PredictionTrailingWeeks);

            var checkInTimes = await db.CheckIns
                .Where(c => c.CheckInTime >= windowStart && c.CheckInTime <= asOf)
                .Select(c => c.CheckInTime)
                .ToListAsync();

            return AttendanceReportService.GroupByDayOfWeek(checkInTimes);
        }
    }
}
