using LoginFormASPCore6.Controllers;

namespace Testing
{
    public class SessionBookingTests
    {
        [Theory]
        [InlineData(10, 0, true)]
        [InlineData(10, 9, true)]
        [InlineData(10, 10, false)]
        [InlineData(10, 11, false)]
        [InlineData(0, 0, false)]
        public void CanBook_RespectsCapacity(int capacity, int currentBookingCount, bool expected)
        {
            Assert.Equal(expected, SessionsController.CanBook(capacity, currentBookingCount));
        }

        [Fact]
        public void GenerateOccurrenceDates_ReturnsOnlyMatchingWeekdaysWithinRange()
        {
            // 2026-03-16 is a Monday.
            var from = new DateTime(2026, 3, 16);
            var dates = SessionsController.GenerateOccurrenceDates(from, new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, weeksToGenerate: 2);

            Assert.All(dates, d => Assert.True(d.DayOfWeek == DayOfWeek.Tuesday || d.DayOfWeek == DayOfWeek.Thursday));
            Assert.Equal(4, dates.Count);
            Assert.All(dates, d => Assert.True(d > from && d <= from.AddDays(14)));
        }

        [Fact]
        public void GenerateOccurrenceDates_NoMatchingWeekdays_ReturnsEmpty()
        {
            var from = new DateTime(2026, 3, 16);
            var dates = SessionsController.GenerateOccurrenceDates(from, Array.Empty<DayOfWeek>(), weeksToGenerate: 4);
            Assert.Empty(dates);
        }
    }
}
