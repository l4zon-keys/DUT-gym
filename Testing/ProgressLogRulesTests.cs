using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;

namespace Testing
{
    public class ProgressLogRulesTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData(19, false)]
        [InlineData(20, true)]
        [InlineData(150, true)]
        [InlineData(300, true)]
        [InlineData(301, false)]
        public void IsValidWeight_ChecksRange(int? weight, bool expected)
        {
            Assert.Equal(expected, ProgressLogRules.IsValidWeight(weight.HasValue ? (decimal?)weight.Value : null));
        }

        [Fact]
        public void BuildProgressMessage_NoTarget_ReturnsGenericMessage()
        {
            var message = ProgressLogRules.BuildProgressMessage(GoalType.WeightLoss, 80, null);
            Assert.Equal("Progress logged.", message);
        }

        [Fact]
        public void BuildProgressMessage_AtTarget_ReturnsReachedMessage()
        {
            var message = ProgressLogRules.BuildProgressMessage(GoalType.WeightLoss, 70, 70);
            Assert.Contains("reached your target", message);
        }

        [Fact]
        public void BuildProgressMessage_WeightLoss_AboveTarget_ReturnsDistanceRemaining()
        {
            var message = ProgressLogRules.BuildProgressMessage(GoalType.WeightLoss, 73, 70);
            Assert.Contains("3kg away from your target", message);
        }

        [Fact]
        public void BuildProgressMessage_WeightLoss_BelowTarget_ReturnsPastTarget()
        {
            var message = ProgressLogRules.BuildProgressMessage(GoalType.WeightLoss, 67, 70);
            Assert.Contains("3kg past your target", message);
        }

        [Fact]
        public void BuildProgressMessage_MuscleGain_BelowTarget_ReturnsDistanceRemaining()
        {
            var message = ProgressLogRules.BuildProgressMessage(GoalType.MuscleGain, 77, 80);
            Assert.Contains("3kg away from your target", message);
        }

        [Fact]
        public void BuildProgressMessage_MuscleGain_AboveTarget_ReturnsPastTarget()
        {
            var message = ProgressLogRules.BuildProgressMessage(GoalType.MuscleGain, 83, 80);
            Assert.Contains("3kg past your target", message);
        }

        [Fact]
        public void BuildProgressMessage_Tone_BelowTarget_ReturnsDistanceRemaining()
        {
            var message = ProgressLogRules.BuildProgressMessage(GoalType.Tone, 77, 80);
            Assert.Contains("3kg away from your target", message);
        }

        [Fact]
        public void BuildProgressMessage_Endurance_AboveTarget_ReturnsPastTarget()
        {
            var message = ProgressLogRules.BuildProgressMessage(GoalType.Endurance, 83, 80);
            Assert.Contains("3kg past your target", message);
        }
    }
}
