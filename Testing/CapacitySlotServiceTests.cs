using LoginFormASPCore6.Services;

namespace Testing
{
    public class CapacitySlotServiceTests
    {
        [Theory]
        [InlineData(10, 5, true)]
        [InlineData(10, 9, true)]
        [InlineData(10, 10, false)]
        [InlineData(10, 15, false)]
        [InlineData(0, 0, false)]
        public void HasOpenSpot_ChecksCapacity(int capacity, int bookedCount, bool expected)
        {
            Assert.Equal(expected, CapacitySlotService.HasOpenSpot(capacity, bookedCount));
        }

        [Fact]
        public void NextWaitlistPosition_EmptyList_ReturnsOne()
        {
            Assert.Equal(1, CapacitySlotService.NextWaitlistPosition(new List<int>()));
        }

        [Fact]
        public void NextWaitlistPosition_ReturnsOneMoreThanMax()
        {
            Assert.Equal(4, CapacitySlotService.NextWaitlistPosition(new[] { 1, 2, 3 }));
        }

        [Fact]
        public void NextWaitlistPosition_IgnoresOrder()
        {
            Assert.Equal(6, CapacitySlotService.NextWaitlistPosition(new[] { 5, 1, 3 }));
        }
    }
}
