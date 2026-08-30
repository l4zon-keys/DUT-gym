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
    }
}
