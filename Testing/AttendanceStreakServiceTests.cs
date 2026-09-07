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

        [Fact]
        public void CountDistinctDaysInWeek_NoVisits_ReturnsZero()
        {
            var days = AttendanceStreakService.CountDistinctDaysInWeek(new List<DateTime>(), new DateTime(2026, 3, 18));
            Assert.Equal(0, days);
        }

        [Fact]
        public void CountDistinctDaysInWeek_CountsDistinctDatesWithinMondayToSunday()
        {
            // 2026-03-18 is a Wednesday; that week runs Mon 2026-03-16 to Sun 2026-03-22.
            var visits = new List<DateTime>
            {
                new(2026, 3, 16, 8, 0, 0),
                new(2026, 3, 16, 18, 0, 0), // same day, twice - should not double count
                new(2026, 3, 18, 9, 0, 0),
                new(2026, 3, 23, 9, 0, 0), // next week - excluded
                new(2026, 3, 9, 9, 0, 0),  // previous week - excluded
            };

            var days = AttendanceStreakService.CountDistinctDaysInWeek(visits, new DateTime(2026, 3, 18));

            Assert.Equal(2, days);
        }

        [Fact]
        public void CalculateAverageVisitMinutes_IgnoresOpenCheckIns()
        {
            var visits = new List<(DateTime In, DateTime? Out)>
            {
                (new DateTime(2026, 3, 1, 8, 0, 0), new DateTime(2026, 3, 1, 9, 0, 0)),  // 60 min
                (new DateTime(2026, 3, 2, 8, 0, 0), new DateTime(2026, 3, 2, 8, 30, 0)), // 30 min
                (new DateTime(2026, 3, 3, 8, 0, 0), null),                               // still open - excluded
            };

            var average = AttendanceStreakService.CalculateAverageVisitMinutes(visits);

            Assert.Equal(45, average);
        }

        [Fact]
        public void CalculateAverageVisitMinutes_NoCompletedVisits_ReturnsZero()
        {
            var visits = new List<(DateTime In, DateTime? Out)> { (DateTime.UtcNow, null) };
            Assert.Equal(0, AttendanceStreakService.CalculateAverageVisitMinutes(visits));
        }

        [Fact]
        public void CalculateTodayMinutes_OpenCheckIn_UsesNowAsEndpoint()
        {
            var checkInTime = new DateTime(2026, 3, 1, 8, 0, 0);
            var now = new DateTime(2026, 3, 1, 8, 45, 0);
            var visits = new List<(DateTime In, DateTime? Out)> { (checkInTime, null) };

            var minutes = AttendanceStreakService.CalculateTodayMinutes(visits, now);

            Assert.Equal(45, minutes);
        }

        [Fact]
        public void CalculateTodayMinutes_MultipleVisits_SumsThem()
        {
            var now = new DateTime(2026, 3, 1, 20, 0, 0);
            var visits = new List<(DateTime In, DateTime? Out)>
            {
                (new DateTime(2026, 3, 1, 6, 0, 0), new DateTime(2026, 3, 1, 6, 30, 0)),
                (new DateTime(2026, 3, 1, 18, 0, 0), null),
            };

            var minutes = AttendanceStreakService.CalculateTodayMinutes(visits, now);

            Assert.Equal(150, minutes);
        }
    }
}
