namespace LoginFormASPCore6.Models
{
    // Shared by any role that needs Admin sign-off before dashboard access -
    // currently Trainer and Staff. Student and Admin are never gated by this.
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public enum TrainerRequestStatus
    {
        Pending,
        Accepted,
        Rejected,
        Cancelled
    }

    public enum GoalType
    {
        WeightLoss,
        MuscleGain,
        GeneralFitness
    }
}
