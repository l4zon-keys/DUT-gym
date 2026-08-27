namespace LoginFormASPCore6.Models
{
    public enum MembershipStatus
    {
        Pending,
        Active,
        Rejected,
        Expired
    }

    public enum PaymentMethod
    {
        Gateway,
        ManualProof
    }

    public enum PaymentStatus
    {
        Pending,
        Verified,
        Rejected
    }
}
