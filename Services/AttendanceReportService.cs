using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    public class AttendanceReport
    {
        // Hour of day (0-23) -> number of check-ins starting in that hour.
        public Dictionary<int, int> PeakHours { get; set; } = new();

        // "yyyy-MM" -> number of check-ins that month.
        public Dictionary<string, int> MonthlyTotals { get; set; } = new();

        public int TotalCheckIns { get; set; }
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

        public async Task<AttendanceReport> BuildReportAsync()
        {
            var checkInTimes = await db.CheckIns.Select(c => c.CheckInTime).ToListAsync();

            return new AttendanceReport
            {
                PeakHours = GroupByHourOfDay(checkInTimes),
                MonthlyTotals = GroupByMonth(checkInTimes),
                TotalCheckIns = checkInTimes.Count
            };
        }
    }
}
