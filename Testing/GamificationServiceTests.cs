using LoginFormASPCore6.Services;

namespace Testing
{
    public class GamificationServiceTests
    {
        [Theory]
        [InlineData(0, 200, 1000, GamificationLevel.Beginner)]
        [InlineData(199, 200, 1000, GamificationLevel.Beginner)]
        [InlineData(200, 200, 1000, GamificationLevel.Regular)]
        [InlineData(999, 200, 1000, GamificationLevel.Regular)]
        [InlineData(1000, 200, 1000, GamificationLevel.BeastMode)]
        [InlineData(5000, 200, 1000, GamificationLevel.BeastMode)]
        public void CalculateLevel_ReturnsExpectedLevel(int totalXp, int regularThreshold, int beastModeThreshold, GamificationLevel expected)
        {
            var level = GamificationService.CalculateLevel(totalXp, regularThreshold, beastModeThreshold);
            Assert.Equal(expected, level);
        }

        [Theory]
        [InlineData(6, 7, true)]
        [InlineData(0, 7, true)]
        [InlineData(6, 6, false)]
        [InlineData(7, 7, false)]
        [InlineData(12, 7, false)]
        public void IsEligibleForEarlyBird_ChecksCutoffHour(int checkInHour, int cutoffHour, bool expected)
        {
            var checkInTime = new DateTime(2026, 1, 1, checkInHour, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expected, GamificationService.IsEligibleForEarlyBird(checkInTime, cutoffHour));
        }

        [Theory]
        [InlineData(4, 5, false)]
        [InlineData(5, 5, true)]
        [InlineData(7, 5, true)]
        public void IsEligibleForFiveDayStreak_ChecksThreshold(int daysThisWeek, int threshold, bool expected)
        {
            Assert.Equal(expected, GamificationService.IsEligibleForFiveDayStreak(daysThisWeek, threshold));
        }

        [Theory]
        [InlineData(4, 5, false)]
        [InlineData(5, 5, true)]
        [InlineData(10, 5, true)]
        public void IsEligibleForZumbaFanatic_ChecksThreshold(int bookingCount, int threshold, bool expected)
        {
            Assert.Equal(expected, GamificationService.IsEligibleForZumbaFanatic(bookingCount, threshold));
        }

        [Theory]
        [InlineData(49, 50, false)]
        [InlineData(50, 50, true)]
        [InlineData(60, 50, true)]
        public void IsChallengeComplete_ChecksTarget(decimal currentValue, decimal targetValue, bool expected)
        {
            Assert.Equal(expected, GamificationService.IsChallengeComplete(currentValue, targetValue));
        }

        [Fact]
        public void RankLeaderboard_OrdersByXpDescendingThenNameAscending()
        {
            var entries = new List<GamificationLeaderboardEntry>
            {
                new() { UserId = 1, EmpName = "Zoe", TotalXp = 50 },
                new() { UserId = 2, EmpName = "Amy", TotalXp = 100 },
                new() { UserId = 3, EmpName = "Bob", TotalXp = 100 },
            };

            var ranked = GamificationService.RankLeaderboard(entries, topN: 10);

            Assert.Equal(new[] { "Amy", "Bob", "Zoe" }, ranked.Select(e => e.EmpName));
        }

        [Fact]
        public void RankLeaderboard_RespectsTopN()
        {
            var entries = Enumerable.Range(1, 5)
                .Select(i => new GamificationLeaderboardEntry { UserId = i, EmpName = $"User{i}", TotalXp = i })
                .ToList();

            var ranked = GamificationService.RankLeaderboard(entries, topN: 2);

            Assert.Equal(2, ranked.Count);
        }
    }
}
