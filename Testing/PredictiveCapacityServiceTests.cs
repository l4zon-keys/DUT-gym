using LoginFormASPCore6.Services;

namespace Testing
{
    public class PredictiveCapacityServiceTests
    {
        [Fact]
        public void RankHoursByHistoricalLoad_OrdersByHourAndClassifiesLevel()
        {
            var byHour = new Dictionary<int, int> { { 9, 80 }, { 6, 10 }, { 18, 50 } };

            var ranked = PredictiveCapacityService.RankHoursByHistoricalLoad(byHour, threshold: 100, moderateAt: 0.5, heavyAt: 0.8);

            Assert.Equal(new[] { 6, 9, 18 }, ranked.Select(r => r.Hour));
            Assert.Equal(CapacityLevel.Light, ranked.First(r => r.Hour == 6).PredictedLevel);
            Assert.Equal(CapacityLevel.Heavy, ranked.First(r => r.Hour == 9).PredictedLevel);
            Assert.Equal(CapacityLevel.Moderate, ranked.First(r => r.Hour == 18).PredictedLevel);
        }

        [Fact]
        public void FindQuietestUpcomingHour_PicksLowestCountAtOrAfterCurrentHour()
        {
            var predictions = new List<BusyHourPrediction>
            {
                new(6, 5, CapacityLevel.Light),
                new(9, 80, CapacityLevel.Heavy),
                new(14, 20, CapacityLevel.Light),
                new(18, 50, CapacityLevel.Moderate),
            };

            var best = PredictiveCapacityService.FindQuietestUpcomingHour(predictions, currentHour: 8);

            Assert.Equal(14, best.Hour);
        }

        [Fact]
        public void FindQuietestUpcomingHour_NoUpcomingHours_FallsBackToWholeDay()
        {
            var predictions = new List<BusyHourPrediction>
            {
                new(6, 5, CapacityLevel.Light),
                new(9, 80, CapacityLevel.Heavy),
            };

            var best = PredictiveCapacityService.FindQuietestUpcomingHour(predictions, currentHour: 22);

            Assert.Equal(6, best.Hour);
        }

        [Fact]
        public void FindQuietestUpcomingHour_NoData_ReturnsCurrentHourFallback()
        {
            var best = PredictiveCapacityService.FindQuietestUpcomingHour(new List<BusyHourPrediction>(), currentHour: 10);

            Assert.Equal(10, best.Hour);
        }
    }
}
