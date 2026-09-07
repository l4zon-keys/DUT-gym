using LoginFormASPCore6.Models;

namespace LoginFormASPCore6.Services
{
    // Pure validation/messaging rules for FitnessGoal progress logging (sprint 1
    // feedback: reject empty/out-of-range submissions, give the student a
    // response that reflects their progress toward their target).
    public static class ProgressLogRules
    {
        public static bool IsValidWeight(decimal? weightKg)
            => weightKg.HasValue && weightKg.Value >= 20 && weightKg.Value <= 300;

        public static string BuildProgressMessage(GoalType goalType, decimal weightKg, decimal? targetWeightKg)
        {
            if (targetWeightKg == null)
            {
                return "Progress logged.";
            }

            var diff = weightKg - targetWeightKg.Value;
            if (Math.Abs(diff) < 0.1m)
            {
                return "Progress logged — you've reached your target weight!";
            }

            if (goalType == GoalType.WeightLoss)
            {
                return diff > 0
                    ? $"Progress logged — you're {diff:0.#}kg away from your target."
                    : $"Progress logged — you're {Math.Abs(diff):0.#}kg past your target weight!";
            }

            // MuscleGain / GeneralFitness: heavier than starting point is progress.
            return diff < 0
                ? $"Progress logged — you're {Math.Abs(diff):0.#}kg away from your target."
                : $"Progress logged — you're {diff:0.#}kg past your target weight!";
        }
    }
}
