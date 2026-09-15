using LoginFormASPCore6.Models;

namespace LoginFormASPCore6.Services
{
    public record DailyCalorieEstimate(int MaintenanceCalories, int TargetCalories, int ProteinG, int CarbsG, int FatG);

    // Calorie/macro estimates and healthy eating tips, built on the existing
    // static Recipe list - no external nutrition API, just standard
    // sports-nutrition formulas and hardcoded constants (fully static, no DI,
    // mirroring MealSuggestionService).
    //
    // The app doesn't collect age or height, so BMR uses fixed, documented
    // assumptions (age 22, typical campus-student height per gender) rather
    // than the full Mifflin-St Jeor inputs - this is an approximation, not a
    // clinical estimate.
    public static class NutritionService
    {
        private const int AssumedAge = 22;
        private const int AssumedHeightMaleCm = 170;
        private const int AssumedHeightFemaleCm = 160;

        public static double CalculateBmr(decimal weightKg, string gender)
        {
            var isMale = string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase);
            var height = isMale ? AssumedHeightMaleCm : AssumedHeightFemaleCm;
            var genderOffset = isMale ? 5 : -161;
            return 10 * (double)weightKg + 6.25 * height - 5 * AssumedAge + genderOffset;
        }

        public static double ApplyActivityMultiplier(double bmr, ActivityLevel? activityLevel)
        {
            var multiplier = activityLevel switch
            {
                ActivityLevel.Sedentary => 1.2,
                ActivityLevel.LightlyActive => 1.375,
                ActivityLevel.VeryActive => 1.725,
                _ => 1.55 // ModeratelyActive, or unset - assume average student activity
            };
            return bmr * multiplier;
        }

        // WeightLoss gets a deficit, MuscleGain a surplus; Tone/Endurance/GeneralFitness
        // stay at maintenance since the app has no separate signal for those goals.
        public static int CalculateTargetCalories(double maintenanceCalories, GoalType goalType) => goalType switch
        {
            GoalType.WeightLoss => (int)Math.Round(maintenanceCalories - 500),
            GoalType.MuscleGain => (int)Math.Round(maintenanceCalories + 300),
            _ => (int)Math.Round(maintenanceCalories)
        };

        public static (int ProteinG, int CarbsG, int FatG) CalculateMacros(int targetCalories, GoalType goalType)
        {
            var (proteinPct, carbsPct, fatPct) = goalType switch
            {
                GoalType.WeightLoss => (0.35, 0.35, 0.30),
                GoalType.MuscleGain => (0.30, 0.45, 0.25),
                _ => (0.25, 0.50, 0.25)
            };

            var proteinG = (int)Math.Round(targetCalories * proteinPct / 4);
            var carbsG = (int)Math.Round(targetCalories * carbsPct / 4);
            var fatG = (int)Math.Round(targetCalories * fatPct / 9);
            return (proteinG, carbsG, fatG);
        }

        public static DailyCalorieEstimate BuildEstimate(decimal weightKg, string gender, GoalType goalType, ActivityLevel? activityLevel)
        {
            var bmr = CalculateBmr(weightKg, gender);
            var maintenance = ApplyActivityMultiplier(bmr, activityLevel);
            var target = CalculateTargetCalories(maintenance, goalType);
            var (proteinG, carbsG, fatG) = CalculateMacros(target, goalType);

            return new DailyCalorieEstimate((int)Math.Round(maintenance), target, proteinG, carbsG, fatG);
        }

        // Static tip bank - null ForGoal means "applies to everyone".
        public static readonly IReadOnlyList<(string Tip, GoalType? ForGoal)> HealthyEatingTips = new List<(string, GoalType?)>
        {
            ("Drink water before meals - it's easy to mistake thirst for hunger.", null),
            ("Aim for a palm-sized portion of protein at each main meal.", null),
            ("Keep a few quick, cheap staples (eggs, oats, tinned beans) on hand for busy days.", null),
            ("Prep a few meals in advance so a tight schedule doesn't mean skipping meals.", null),
            ("Fill half your plate with vegetables when you can - they're filling and cheap per gram.", null),

            ("A moderate calorie deficit (not a crash diet) is easier to sustain and protects muscle mass.", GoalType.WeightLoss),
            ("Prioritise protein when cutting calories - it keeps you fuller for longer.", GoalType.WeightLoss),

            ("Spread your protein across the day rather than one big serving - it supports muscle repair better.", GoalType.MuscleGain),
            ("Eat close to your workout (before or after) to fuel and recover.", GoalType.MuscleGain),

            ("Carbs are your main fuel for high-rep or cardio-heavy sessions - don't cut them too low.", GoalType.Endurance),
            ("Rehydrate with water or a pinch-of-salt drink after long or sweaty sessions.", GoalType.Endurance),

            ("Consistency in your eating pattern matters more for toning than any single meal.", GoalType.Tone),
            ("Pair resistance training with enough protein - toning is still a muscle-building process.", GoalType.Tone),

            ("Little and often works too - three solid meals is a fine default if you're not chasing a specific goal.", GoalType.GeneralFitness),
        };

        public static List<string> GetTipsForGoal(GoalType? goalType, int count = 5)
            => HealthyEatingTips
                .Where(t => t.ForGoal == null || t.ForGoal == goalType)
                .OrderBy(t => t.ForGoal == null) // goal-specific tips first
                .Select(t => t.Tip)
                .Take(count)
                .ToList();
    }
}
