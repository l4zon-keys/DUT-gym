namespace LoginFormASPCore6.Models
{
    public enum MembershipStatus
    {
        Pending,
        Active,
        Rejected,
        Expired
    }

    // No real payment processor is ever integrated (no gateway API, no card
    // processing, no webhook). "Paying" is a state machine the app decides itself
    // based on the chosen method - see the settlement rule on PaymentStatus below.
    public enum PaymentMethod
    {
        Cash,
        Card,
        Eft,
        MobileMoney
    }

    public enum PaymentStatus
    {
        // Cash starts here and stays here until an admin confirms the money was
        // physically received.
        Pending,
        // Card/Eft/MobileMoney go straight here - there's no real processor to wait
        // on, so they settle immediately.
        Paid,
        Failed,
        Refunded,
        PartiallyRefunded
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
