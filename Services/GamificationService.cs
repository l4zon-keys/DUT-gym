using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    public enum GamificationLevel
    {
        Beginner,
        Regular,
        BeastMode
    }

    public class GamificationLeaderboardEntry
    {
        public int UserId { get; set; }
        public string EmpName { get; set; } = null!;
        public int TotalXp { get; set; }
    }

    // Small view-model for the _XpLevelBadge partial.
    public class XpLevelSummary
    {
        public int TotalXp { get; set; }
        public GamificationLevel Level { get; set; }
        public int RegularLevelThreshold { get; set; }
        public int BeastModeLevelThreshold { get; set; }
    }

    // XP, levels, badges, challenges, and (display-only) rewards. Calculation/
    // eligibility logic is pure static methods so it's unit-testable without a
    // DbContext; instance methods fetch data and hand it to them, mirroring
    // GymCapacityService's split.
    public class GamificationService
    {
        private readonly MyDbContext db;
        private readonly IConfiguration configuration;

        public GamificationService(MyDbContext db, IConfiguration configuration)
        {
            this.db = db;
            this.configuration = configuration;
        }

        // --- Configured point values / thresholds ---------------------------

        public int CheckInPoints => GetInt("XpPerCheckIn", 10);
        public int GoalSetPoints => GetInt("XpPerGoalSet", 20);
        public int ProgressLoggedPoints => GetInt("XpPerProgressLog", 15);
        public int ClassBookedPoints => GetInt("XpPerClassBooked", 15);
        public int StreakMilestonePoints => GetInt("XpPerStreakMilestone", 25);
        public int ChallengeCompletedPoints => GetInt("XpPerChallengeCompleted", 100);
        public int RegularLevelThreshold => GetInt("RegularLevelThreshold", 200);
        public int BeastModeLevelThreshold => GetInt("BeastModeLevelThreshold", 1000);
        public int EarlyBirdCutoffHour => GetInt("EarlyBirdCutoffHour", 7);
        public int FiveDayStreakDays => GetInt("FiveDayStreakDays", 5);
        public int ZumbaFanaticBookingCount => GetInt("ZumbaFanaticBookingCount", 5);

        private int GetInt(string key, int fallback) =>
            configuration.GetValue<int?>($"Gamification:{key}") ?? fallback;

        // --- Pure calculation / eligibility (unit-testable) -----------------

        public static GamificationLevel CalculateLevel(int totalXp, int regularThreshold = 200, int beastModeThreshold = 1000)
            => totalXp >= beastModeThreshold ? GamificationLevel.BeastMode
             : totalXp >= regularThreshold ? GamificationLevel.Regular
             : GamificationLevel.Beginner;

        public static bool IsEligibleForEarlyBird(DateTime checkInTimeUtc, int cutoffHour = 7)
            => checkInTimeUtc.Hour < cutoffHour;

        public static bool IsEligibleForFiveDayStreak(int consecutiveDaysThisWeek, int thresholdDays = 5)
            => consecutiveDaysThisWeek >= thresholdDays;

        public static bool IsEligibleForZumbaFanatic(int zumbaBookingCount, int threshold = 5)
            => zumbaBookingCount >= threshold;

        public static bool IsChallengeComplete(decimal currentValue, decimal targetValue)
            => currentValue >= targetValue;

        public static List<GamificationLeaderboardEntry> RankLeaderboard(IEnumerable<GamificationLeaderboardEntry> entries, int topN)
            => entries.OrderByDescending(e => e.TotalXp).ThenBy(e => e.EmpName).Take(topN).ToList();

        // --- XP ledger --------------------------------------------------------

        // Returns false (no-op) if an XpEvent for this exact UserId+Reason+SourceId
        // already exists, so callers can safely call this from a hook point that
        // might run more than once for the same underlying event.
        public async Task<bool> AwardXpAsync(int userId, int points, XpReason reason, int? sourceId = null)
        {
            if (sourceId.HasValue)
            {
                var alreadyAwarded = await db.XpEvents.AnyAsync(e =>
                    e.UserId == userId && e.Reason == reason && e.SourceId == sourceId);
                if (alreadyAwarded) return false;
            }

            db.XpEvents.Add(new XpEvent
            {
                UserId = userId,
                Points = points,
                Reason = reason,
                SourceId = sourceId
            });
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalXpAsync(int userId)
            => await db.XpEvents.Where(e => e.UserId == userId).SumAsync(e => (int?)e.Points) ?? 0;

        public async Task<GamificationLevel> GetLevelAsync(int userId)
        {
            var totalXp = await GetTotalXpAsync(userId);
            return CalculateLevel(totalXp, RegularLevelThreshold, BeastModeLevelThreshold);
        }

        public async Task<XpLevelSummary> GetXpLevelSummaryAsync(int userId)
        {
            var totalXp = await GetTotalXpAsync(userId);
            return new XpLevelSummary
            {
                TotalXp = totalXp,
                Level = CalculateLevel(totalXp, RegularLevelThreshold, BeastModeLevelThreshold),
                RegularLevelThreshold = RegularLevelThreshold,
                BeastModeLevelThreshold = BeastModeLevelThreshold
            };
        }

        // --- Badges -------------------------------------------------------------

        public async Task<bool> AwardBadgeIfEligibleAsync(int userId, string badgeCode)
        {
            var badge = await db.Badges.FirstOrDefaultAsync(b => b.Code == badgeCode);
            if (badge == null) return false;

            var alreadyEarned = await db.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badge.Id);
            if (alreadyEarned) return false;

            db.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = badge.Id });
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserBadge>> GetEarnedBadgesAsync(int userId)
            => await db.UserBadges
                .Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .OrderByDescending(ub => ub.EarnedAt)
                .ToListAsync();

        // --- Rewards (display-only) ---------------------------------------------

        public async Task<List<UserReward>> GetEarnedRewardsAsync(int userId)
            => await db.UserRewards
                .Include(ur => ur.Reward)
                .Where(ur => ur.UserId == userId)
                .OrderByDescending(ur => ur.EarnedAt)
                .ToListAsync();

        // Awards every Reward the user's current total XP now qualifies for and
        // hasn't already earned. Call this after any AwardXpAsync so unlocks
        // happen as a side effect of crossing a threshold, not on a separate pass.
        public async Task EvaluateAndAwardRewardsAsync(int userId)
        {
            var totalXp = await GetTotalXpAsync(userId);
            var earnedRewardIds = await db.UserRewards
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RewardId)
                .ToListAsync();

            var newlyQualifying = await db.Rewards
                .Where(r => r.RequiredXp <= totalXp && !earnedRewardIds.Contains(r.Id))
                .ToListAsync();

            if (newlyQualifying.Count == 0) return;

            foreach (var reward in newlyQualifying)
            {
                db.UserRewards.Add(new UserReward { UserId = userId, RewardId = reward.Id });
            }
            await db.SaveChangesAsync();
        }

        // --- Leaderboards -----------------------------------------------------

        public async Task<List<GamificationLeaderboardEntry>> GetXpLeaderboardAsync(DateTime periodStart, DateTime periodEnd, int topN = 10)
        {
            var counts = await db.XpEvents
                .Where(e => e.CreatedAt >= periodStart && e.CreatedAt < periodEnd)
                .GroupBy(e => e.UserId)
                .Select(g => new { UserId = g.Key, TotalXp = g.Sum(e => e.Points) })
                .ToListAsync();

            return await BuildLeaderboardAsync(counts.Select(c => (c.UserId, c.TotalXp)), topN);
        }

        public async Task<List<GamificationLeaderboardEntry>> GetFriendsLeaderboardAsync(int userId, int topN = 10)
        {
            var friendIds = await db.Friendships
                .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserId == userId || f.FriendUserId == userId))
                .Select(f => f.UserId == userId ? f.FriendUserId : f.UserId)
                .ToListAsync();

            var ids = friendIds.Append(userId).Distinct().ToList();

            var counts = await db.XpEvents
                .Where(e => ids.Contains(e.UserId))
                .GroupBy(e => e.UserId)
                .Select(g => new { UserId = g.Key, TotalXp = g.Sum(e => e.Points) })
                .ToListAsync();

            var withZeros = ids
                .GroupJoin(counts, id => id, c => c.UserId, (id, matches) => (id, matches.Select(m => m.TotalXp).DefaultIfEmpty(0).Sum()));

            return await BuildLeaderboardAsync(withZeros, topN);
        }

        private async Task<List<GamificationLeaderboardEntry>> BuildLeaderboardAsync(IEnumerable<(int UserId, int TotalXp)> counts, int topN)
        {
            var userIds = counts.Select(c => c.UserId).ToList();
            var names = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.EmpName);

            var entries = counts.Select(c => new GamificationLeaderboardEntry
            {
                UserId = c.UserId,
                EmpName = names.GetValueOrDefault(c.UserId, "Unknown"),
                TotalXp = c.TotalXp
            });

            return RankLeaderboard(entries, topN);
        }
    }
}
