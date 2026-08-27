using LoginFormASPCore6.Services;

namespace Testing
{
    public class GymCapacityServiceTests
    {
        [Theory]
        [InlineData(10, 100, 0.5, 0.8, CapacityLevel.Light)]
        [InlineData(50, 100, 0.5, 0.8, CapacityLevel.Moderate)]
        [InlineData(79, 100, 0.5, 0.8, CapacityLevel.Moderate)]
        [InlineData(80, 100, 0.5, 0.8, CapacityLevel.Heavy)]
        [InlineData(150, 100, 0.5, 0.8, CapacityLevel.Heavy)]
        [InlineData(0, 100, 0.5, 0.8, CapacityLevel.Light)]
        [InlineData(5, 0, 0.5, 0.8, CapacityLevel.Light)]
        public void CalculateLevel_ReturnsExpectedLevel(int occupancy, int threshold, double moderateAt, double heavyAt, CapacityLevel expected)
        {
            var level = GymCapacityService.CalculateLevel(occupancy, threshold, moderateAt, heavyAt);

            Assert.Equal(expected, level);
        }
    }
}
