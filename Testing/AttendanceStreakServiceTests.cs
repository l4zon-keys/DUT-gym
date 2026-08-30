using LoginFormASPCore6.Services;

namespace Testing
{
    public class AttendanceStreakServiceTests
    {
        [Fact]
        public void CalculateMonthlyStreak_NoVisits_ReturnsZero()
        {
            var streak = AttendanceStreakService.CalculateMonthlyStreak(new List<DateTime>(), new DateTime(2026, 3, 15));
            Assert.Equal(0, streak);
        }

        [Fact]
        public void CalculateMonthlyStreak_CurrentMonthOnly_ReturnsOne()
        {
            var visits = new List<DateTime> { new DateTime(2026, 3, 5) };
            var streak = AttendanceStreakService.CalculateMonthlyStreak(visits, new DateTime(2026, 3, 15));
            Assert.Equal(1, streak);
        }

        [Fact]
        public void CalculateMonthlyStreak_ThreeConsecutiveMonths_ReturnsThree()
        {
            var visits = new List<DateTime>
            {
                new DateTime(2026, 1, 10),
                new DateTime(2026, 2, 20),
                new DateTime(2026, 3, 5),
            };
            var streak = AttendanceStreakService.CalculateMonthlyStreak(visits, new DateTime(2026, 3, 15));
            Assert.Equal(3, streak);
        }

        [Fact]
        public void CalculateMonthlyStreak_GapInMonths_BreaksStreak()
        {
            var visits = new List<DateTime>
            {
                new DateTime(2026, 1, 10),
                // February has no visits.
                new DateTime(2026, 3, 5),
            };
            var streak = AttendanceStreakService.CalculateMonthlyStreak(visits, new DateTime(2026, 3, 15));
            Assert.Equal(1, streak);
        }

        [Fact]
        public void CalculateMonthlyStreak_NoVisitThisMonth_ReturnsZeroEvenWithPastStreak()
        {
            var visits = new List<DateTime>
            {
                new DateTime(2026, 1, 10),
                new DateTime(2026, 2, 20),
            };
            // Asking as of March, but March has no visits yet.
            var streak = AttendanceStreakService.CalculateMonthlyStreak(visits, new DateTime(2026, 3, 15));
            Assert.Equal(0, streak);
        }

        [Theory]
        [InlineData(9, 10, false)]
        [InlineData(10, 10, true)]
        [InlineData(15, 10, true)]
        [InlineData(0, 10, false)]
        public void IsEligibleForCertificate_ChecksThreshold(int visits, int threshold, bool expected)
        {
            Assert.Equal(expected, AttendanceStreakService.IsEligibleForCertificate(visits, threshold));
        }

        [Fact]
        public void RankLeaderboard_OrdersByVisitCountDescending()
        {
            var entries = new List<LeaderboardEntry>
            {
                new() { UserId = 1, EmpName = "Alice", VisitCount = 5 },
                new() { UserId = 2, EmpName = "Bob", VisitCount = 12 },
                new() { UserId = 3, EmpName = "Carol", VisitCount = 8 },
            };

            var ranked = AttendanceStreakService.RankLeaderboard(entries, topN: 10);

            Assert.Equal(new[] { "Bob", "Carol", "Alice" }, ranked.Select(e => e.EmpName));
        }

        [Fact]
        public void RankLeaderboard_RespectsTopN()
        {
            var entries = Enumerable.Range(1, 20)
                .Select(i => new LeaderboardEntry { UserId = i, EmpName = $"User{i}", VisitCount = i })
                .ToList();

            var ranked = AttendanceStreakService.RankLeaderboard(entries, topN: 3);

            Assert.Equal(3, ranked.Count);
            Assert.Equal("User20", ranked[0].EmpName);
        }

        [Fact]
        public void RankLeaderboard_TiesBrokenByName()
        {
            var entries = new List<LeaderboardEntry>
            {
                new() { UserId = 1, EmpName = "Zed", VisitCount = 5 },
                new() { UserId = 2, EmpName = "Amy", VisitCount = 5 },
            };

            var ranked = AttendanceStreakService.RankLeaderboard(entries, topN: 10);

            Assert.Equal("Amy", ranked[0].EmpName);
        }
    }
}
