using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;

namespace Testing
{
    public class NutritionServiceTests
    {
        [Theory]
        [InlineData(70, "Male")]
        [InlineData(60, "Female")]
        public void CalculateBmr_ReturnsPositiveSaneValue(decimal weightKg, string gender)
        {
            var bmr = NutritionService.CalculateBmr(weightKg, gender);
            Assert.InRange(bmr, 800, 3000);
        }

        [Fact]
        public void CalculateBmr_MaleHigherThanFemale_AtSameWeight()
        {
            var male = NutritionService.CalculateBmr(70, "Male");
            var female = NutritionService.CalculateBmr(70, "Female");
            Assert.True(male > female);
        }

        [Theory]
        [InlineData(ActivityLevel.Sedentary, 1.2)]
        [InlineData(ActivityLevel.LightlyActive, 1.375)]
        [InlineData(ActivityLevel.ModeratelyActive, 1.55)]
        [InlineData(ActivityLevel.VeryActive, 1.725)]
        [InlineData(null, 1.55)]
        public void ApplyActivityMultiplier_UsesExpectedFactor(ActivityLevel? level, double expectedMultiplier)
        {
            var result = NutritionService.ApplyActivityMultiplier(2000, level);
            Assert.Equal(2000 * expectedMultiplier, result, 3);
        }

        [Fact]
        public void CalculateTargetCalories_WeightLoss_IsBelowMaintenance()
        {
            var target = NutritionService.CalculateTargetCalories(2500, GoalType.WeightLoss);
            Assert.True(target < 2500);
        }

        [Fact]
        public void CalculateTargetCalories_MuscleGain_IsAboveMaintenance()
        {
            var target = NutritionService.CalculateTargetCalories(2500, GoalType.MuscleGain);
            Assert.True(target > 2500);
        }

        [Theory]
        [InlineData(GoalType.GeneralFitness)]
        [InlineData(GoalType.Tone)]
        [InlineData(GoalType.Endurance)]
        public void CalculateTargetCalories_OtherGoals_EqualsMaintenance(GoalType goalType)
        {
            var target = NutritionService.CalculateTargetCalories(2500, goalType);
            Assert.Equal(2500, target);
        }

        [Theory]
        [InlineData(GoalType.WeightLoss)]
        [InlineData(GoalType.MuscleGain)]
        [InlineData(GoalType.GeneralFitness)]
        [InlineData(GoalType.Tone)]
        [InlineData(GoalType.Endurance)]
        public void CalculateMacros_GramsApproximatelyMatchCalories(GoalType goalType)
        {
            const int targetCalories = 2400;
            var (proteinG, carbsG, fatG) = NutritionService.CalculateMacros(targetCalories, goalType);

            var impliedCalories = proteinG * 4 + carbsG * 4 + fatG * 9;

            Assert.InRange(impliedCalories, targetCalories - 50, targetCalories + 50);
        }

        [Fact]
        public void BuildEstimate_ReturnsConsistentTotals()
        {
            var estimate = NutritionService.BuildEstimate(75, "Male", GoalType.MuscleGain, ActivityLevel.ModeratelyActive);

            Assert.True(estimate.TargetCalories > estimate.MaintenanceCalories);
            Assert.True(estimate.ProteinG > 0);
            Assert.True(estimate.CarbsG > 0);
            Assert.True(estimate.FatG > 0);
        }

        [Fact]
        public void GetTipsForGoal_PrioritizesGoalSpecificTips()
        {
            var tips = NutritionService.GetTipsForGoal(GoalType.MuscleGain, count: 2);

            Assert.Equal(2, tips.Count);
            Assert.All(tips, tip =>
            {
                var forGoal = NutritionService.HealthyEatingTips.First(t => t.Tip == tip).ForGoal;
                Assert.Equal(GoalType.MuscleGain, forGoal);
            });
        }

        [Fact]
        public void GetTipsForGoal_NullGoal_ReturnsOnlyGenericTips()
        {
            var tips = NutritionService.GetTipsForGoal(null, count: 10);

            Assert.NotEmpty(tips);
            Assert.All(tips, tip =>
            {
                var forGoal = NutritionService.HealthyEatingTips.First(t => t.Tip == tip).ForGoal;
                Assert.Null(forGoal);
            });
        }
    }
}
