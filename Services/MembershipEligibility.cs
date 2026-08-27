using LoginFormASPCore6.Models;

namespace LoginFormASPCore6.Services
{
    // Pulled out of StaffController so the check-in eligibility rule is unit-testable
    // without a DbContext.
    public static class MembershipEligibility
    {
        public static bool CanCheckIn(Membership? membership, DateTime today)
        {
            if (membership == null) return false;
            if (membership.Status != MembershipStatus.Active) return false;
            if (membership.ExpiryDate == null) return false;
            return membership.ExpiryDate.Value.Date >= today.Date;
        }
    }
}
