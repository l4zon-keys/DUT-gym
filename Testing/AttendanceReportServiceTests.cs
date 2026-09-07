using LoginFormASPCore6.Services;

namespace Testing
{
    public class AttendanceReportServiceTests
    {
        [Fact]
        public void GroupByHourOfDay_ReturnsAll24Hours_EvenWithNoData()
        {
            var result = AttendanceReportService.GroupByHourOfDay(new List<DateTime>());

            Assert.Equal(24, result.Count);
            Assert.All(result.Values, v => Assert.Equal(0, v));
        }

        [Fact]
        public void GroupByHourOfDay_CountsCorrectHour()
        {
            var times = new List<DateTime>
            {
                new DateTime(2026, 1, 1, 7, 0, 0),
                new DateTime(2026, 1, 2, 7, 30, 0),
                new DateTime(2026, 1, 3, 18, 0, 0),
            };

            var result = AttendanceReportService.GroupByHourOfDay(times);

            Assert.Equal(2, result[7]);
            Assert.Equal(1, result[18]);
            Assert.Equal(0, result[12]);
        }

        [Fact]
        public void GroupByMonth_GroupsAndSortsChronologically()
        {
            var times = new List<DateTime>
            {
                new DateTime(2026, 3, 1),
                new DateTime(2026, 1, 15),
                new DateTime(2026, 1, 20),
                new DateTime(2026, 2, 10),
            };

            var result = AttendanceReportService.GroupByMonth(times);

            Assert.Equal(new[] { "2026-01", "2026-02", "2026-03" }, result.Keys);
            Assert.Equal(2, result["2026-01"]);
            Assert.Equal(1, result["2026-02"]);
            Assert.Equal(1, result["2026-03"]);
        }

        [Fact]
        public void GroupByMonth_NoData_ReturnsEmpty()
        {
            var result = AttendanceReportService.GroupByMonth(new List<DateTime>());
            Assert.Empty(result);
        }

        [Fact]
        public void GroupByDayOfWeek_ReturnsAll7Days_EvenWithNoData()
        {
            var result = AttendanceReportService.GroupByDayOfWeek(new List<DateTime>());

            Assert.Equal(7, result.Count);
            Assert.All(result.Values, v => Assert.Equal(0, v));
        }

        [Fact]
        public void GroupByDayOfWeek_CountsCorrectDay()
        {
            var times = new List<DateTime>
            {
                new DateTime(2026, 3, 16), // Monday
                new DateTime(2026, 3, 23), // Monday
                new DateTime(2026, 3, 18), // Wednesday
            };

            var result = AttendanceReportService.GroupByDayOfWeek(times);

            Assert.Equal(2, result[DayOfWeek.Monday]);
            Assert.Equal(1, result[DayOfWeek.Wednesday]);
            Assert.Equal(0, result[DayOfWeek.Friday]);
        }

        [Fact]
        public void CalculateAverageMinutes_IgnoresOpenCheckIns()
        {
            var visits = new List<(DateTime In, DateTime? Out)>
            {
                (new DateTime(2026, 3, 1, 8, 0, 0), new DateTime(2026, 3, 1, 9, 0, 0)),
                (new DateTime(2026, 3, 2, 8, 0, 0), null),
            };

            var average = AttendanceReportService.CalculateAverageMinutes(visits);

            Assert.Equal(60, average);
        }

        [Fact]
        public void CalculateAverageMinutes_NoCompletedVisits_ReturnsZero()
        {
            var visits = new List<(DateTime In, DateTime? Out)> { (DateTime.UtcNow, null) };
            Assert.Equal(0, AttendanceReportService.CalculateAverageMinutes(visits));
        }
    }
}
