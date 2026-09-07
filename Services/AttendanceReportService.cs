using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    public class AttendanceReport
    {
        // Hour of day (0-23) -> number of check-ins starting in that hour.
        public Dictionary<int, int> PeakHours { get; set; } = new();

        // Day of week -> number of check-ins starting on that weekday.
        public Dictionary<DayOfWeek, int> PeakDays { get; set; } = new();

        // "yyyy-MM" -> number of check-ins that month.
        public Dictionary<string, int> MonthlyTotals { get; set; } = new();

        public int TotalCheckIns { get; set; }

        // Average minutes spent per visit, over visits with a recorded check-out.
        public double AverageVisitMinutes { get; set; }
    }

    // Admin attendance/usage reporting (PB-8). Grouping logic is pure (plain
    // DateTime lists in, dictionaries out) so it's unit-testable without a DB.
    public class AttendanceReportService
    {
        private readonly MyDbContext db;

        public AttendanceReportService(MyDbContext db)
        {
            this.db = db;
        }

        public static Dictionary<int, int> GroupByHourOfDay(IEnumerable<DateTime> checkInTimes)
        {
            var result = Enumerable.Range(0, 24).ToDictionary(h => h, _ => 0);
            foreach (var time in checkInTimes)
            {
                result[time.Hour]++;
            }
            return result;
        }

        public static Dictionary<string, int> GroupByMonth(IEnumerable<DateTime> checkInTimes)
        {
            return checkInTimes
                .GroupBy(t => $"{t.Year:D4}-{t.Month:D2}")
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public static Dictionary<DayOfWeek, int> GroupByDayOfWeek(IEnumerable<DateTime> checkInTimes)
        {
            var result = Enum.GetValues<DayOfWeek>().ToDictionary(d => d, _ => 0);
            foreach (var time in checkInTimes)
            {
                result[time.DayOfWeek]++;
            }
            return result;
        }

        public static double CalculateAverageMinutes(IEnumerable<(DateTime In, DateTime? Out)> visits)
        {
            var completed = visits
                .Where(v => v.Out.HasValue)
                .Select(v => (v.Out!.Value - v.In).TotalMinutes)
                .ToList();
            return completed.Count == 0 ? 0 : completed.Average();
        }

        public async Task<AttendanceReport> BuildReportAsync()
        {
            var checkIns = await db.CheckIns.Select(c => new { c.CheckInTime, c.CheckOutTime }).ToListAsync();
            var checkInTimes = checkIns.Select(c => c.CheckInTime).ToList();

            return new AttendanceReport
            {
                PeakHours = GroupByHourOfDay(checkInTimes),
                PeakDays = GroupByDayOfWeek(checkInTimes),
                MonthlyTotals = GroupByMonth(checkInTimes),
                TotalCheckIns = checkInTimes.Count,
                AverageVisitMinutes = CalculateAverageMinutes(checkIns.Select(c => (c.CheckInTime, c.CheckOutTime)))
            };
        }
    }
}
