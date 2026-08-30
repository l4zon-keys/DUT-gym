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
