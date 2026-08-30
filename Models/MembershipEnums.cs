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

    public enum PersonalTrainerOption
    {
        None,
        OnceAWeek,
        TwiceAWeek
    }

    // TODO: placeholder rates - swap for the real figures once confirmed.
    public static class PersonalTrainerPricing
    {
        public static readonly IReadOnlyDictionary<PersonalTrainerOption, decimal> Fees = new Dictionary<PersonalTrainerOption, decimal>
        {
            [PersonalTrainerOption.None] = 0m,
            [PersonalTrainerOption.OnceAWeek] = 150m,
            [PersonalTrainerOption.TwiceAWeek] = 250m,
        };

        public static string GetLabel(PersonalTrainerOption option) => option switch
        {
            PersonalTrainerOption.OnceAWeek => $"Personal trainer - once a week (+R{Fees[option]:0.00}/semester)",
            PersonalTrainerOption.TwiceAWeek => $"Personal trainer - twice a week (+R{Fees[option]:0.00}/semester)",
            _ => "No personal trainer"
        };
    }
}
