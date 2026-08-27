using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;

namespace Testing
{
    public class MembershipEligibilityTests
    {
        private static readonly DateTime Today = new(2026, 8, 27);

        [Fact]
        public void CanCheckIn_NullMembership_ReturnsFalse()
        {
            Assert.False(MembershipEligibility.CanCheckIn(null, Today));
        }

        [Fact]
        public void CanCheckIn_PendingMembership_ReturnsFalse()
        {
            var membership = new Membership { Status = MembershipStatus.Pending, ExpiryDate = Today.AddMonths(1) };

            Assert.False(MembershipEligibility.CanCheckIn(membership, Today));
        }

        [Fact]
        public void CanCheckIn_ActiveButExpired_ReturnsFalse()
        {
            var membership = new Membership { Status = MembershipStatus.Active, ExpiryDate = Today.AddDays(-1) };

            Assert.False(MembershipEligibility.CanCheckIn(membership, Today));
        }

        [Fact]
        public void CanCheckIn_ActiveAndNotExpired_ReturnsTrue()
        {
            var membership = new Membership { Status = MembershipStatus.Active, ExpiryDate = Today.AddDays(1) };

            Assert.True(MembershipEligibility.CanCheckIn(membership, Today));
        }

        [Fact]
        public void CanCheckIn_ExpiresToday_ReturnsTrue()
        {
            var membership = new Membership { Status = MembershipStatus.Active, ExpiryDate = Today };

            Assert.True(MembershipEligibility.CanCheckIn(membership, Today));
        }
    }
}
