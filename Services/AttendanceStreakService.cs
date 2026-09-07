using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    public class LeaderboardEntry
    {
        public int UserId { get; set; }
        public string EmpName { get; set; } = null!;
        public int VisitCount { get; set; }
    }

    // Attendance streaks, "Member of the Month" leaderboard, and certificate
    // eligibility (PB-13/14). The calculation methods are pure (no DB access) so
    // they're unit-testable directly; the instance methods just fetch data and
    // hand it to them.
    public class AttendanceStreakService
    {
        private readonly MyDbContext db;

        public AttendanceStreakService(MyDbContext db)
        {
            this.db = db;
        }

        // Consecutive whole months, counting back from asOfUtc's month, in which the
        // user had at least one check-in. Returns 0 if the current month has none.
        public static int CalculateMonthlyStreak(IEnumerable<DateTime> checkInTimes, DateTime asOfUtc)
        {
            var monthsWithVisits = checkInTimes
                .Select(t => new DateTime(t.Year, t.Month, 1))
                .ToHashSet();

            var streak = 0;
            var cursor = new DateTime(asOfUtc.Year, asOfUtc.Month, 1);
            while (monthsWithVisits.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddMonths(-1);
            }
            return streak;
        }

        // A student qualifies for a "Consistency Certificate" once they've visited
        // at least `threshold` times in the given calendar month.
        public static bool IsEligibleForCertificate(int visitsThisMonth, int threshold = 10)
            => visitsThisMonth >= threshold;

        public static List<LeaderboardEntry> RankLeaderboard(IEnumerable<LeaderboardEntry> entries, int topN)
            => entries.OrderByDescending(e => e.VisitCount).ThenBy(e => e.EmpName).Take(topN).ToList();

        // Distinct calendar dates (in checkInTimes) that fall within the Mon-Sun
        // week containing asOfUtc.
        public static int CountDistinctDaysInWeek(IEnumerable<DateTime> checkInTimes, DateTime asOfUtc)
        {
            var weekStart = StartOfWeek(asOfUtc.Date);
            var weekEnd = weekStart.AddDays(7);
            return checkInTimes
                .Where(t => t.Date >= weekStart && t.Date < weekEnd)
                .Select(t => t.Date)
                .Distinct()
                .Count();
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            var diff = (7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return date.AddDays(-diff);
        }

        // Average minutes spent per visit, counting only visits that have both a
        // check-in and a check-out time.
        public static double CalculateAverageVisitMinutes(IEnumerable<(DateTime In, DateTime? Out)> visits)
        {
            var completed = visits
                .Where(v => v.Out.HasValue)
                .Select(v => (v.Out!.Value - v.In).TotalMinutes)
                .ToList();
            return completed.Count == 0 ? 0 : completed.Average();
        }

        // Minutes spent so far today: the open check-in's elapsed time if still
        // inside, otherwise today's completed visit duration, otherwise 0.
        public static double CalculateTodayMinutes(IEnumerable<(DateTime In, DateTime? Out)> todaysVisits, DateTime nowUtc)
        {
            double total = 0;
            foreach (var visit in todaysVisits)
            {
                total += ((visit.Out ?? nowUtc) - visit.In).TotalMinutes;
            }
            return total;
        }

        public async Task<int> GetDaysAttendedThisWeekAsync(int userId, DateTime? asOfUtc = null)
        {
            var asOf = asOfUtc ?? DateTime.UtcNow;
            var weekStart = StartOfWeek(asOf.Date);
            var weekEnd = weekStart.AddDays(7);
            var checkInTimes = await db.CheckIns
                .Where(c => c.UserId == userId && c.CheckInTime >= weekStart && c.CheckInTime < weekEnd)
                .Select(c => c.CheckInTime)
                .ToListAsync();
            return CountDistinctDaysInWeek(checkInTimes, asOf);
        }

        public async Task<double> GetAverageVisitMinutesAsync(int userId)
        {
            var visits = await db.CheckIns
                .Where(c => c.UserId == userId)
                .Select(c => new { c.CheckInTime, c.CheckOutTime })
                .ToListAsync();
            return CalculateAverageVisitMinutes(visits.Select(v => (v.CheckInTime, v.CheckOutTime)));
        }

        public async Task<double> GetTodayMinutesAsync(int userId, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var today = now.Date;
            var visits = await db.CheckIns
                .Where(c => c.UserId == userId && c.CheckInTime >= today && c.CheckInTime < today.AddDays(1))
                .Select(c => new { c.CheckInTime, c.CheckOutTime })
                .ToListAsync();
            return CalculateTodayMinutes(visits.Select(v => (v.CheckInTime, v.CheckOutTime)), now);
        }

        public async Task<int> GetMonthlyStreakAsync(int userId, DateTime? asOfUtc = null)
        {
            var asOf = asOfUtc ?? DateTime.UtcNow;
            var checkInTimes = await db.CheckIns.Where(c => c.UserId == userId).Select(c => c.CheckInTime).ToListAsync();
            return CalculateMonthlyStreak(checkInTimes, asOf);
        }

        public async Task<int> GetVisitCountForMonthAsync(int userId, DateTime monthUtc)
        {
            var start = new DateTime(monthUtc.Year, monthUtc.Month, 1);
            var end = start.AddMonths(1);
            return await db.CheckIns.CountAsync(c => c.UserId == userId && c.CheckInTime >= start && c.CheckInTime < end);
        }

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(DateTime monthUtc, int topN = 10)
        {
            var start = new DateTime(monthUtc.Year, monthUtc.Month, 1);
            var end = start.AddMonths(1);

            // Group/count first (translates cleanly), then look up names separately -
            // navigating a related entity inside a GroupBy projection is unreliable
            // across EF Core providers.
            var counts = await db.CheckIns
                .Where(c => c.CheckInTime >= start && c.CheckInTime < end)
                .GroupBy(c => c.UserId)
                .Select(g => new { UserId = g.Key, VisitCount = g.Count() })
                .ToListAsync();

            var userIds = counts.Select(c => c.UserId).ToList();
            var names = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.EmpName);

            var entries = counts.Select(c => new LeaderboardEntry
            {
                UserId = c.UserId,
                EmpName = names.GetValueOrDefault(c.UserId, "Unknown"),
                VisitCount = c.VisitCount
            });

            return RankLeaderboard(entries, topN);
        }
    }
}
