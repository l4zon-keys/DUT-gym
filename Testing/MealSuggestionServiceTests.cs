using LoginFormASPCore6.Services;

namespace Testing
{
    public class MealSuggestionServiceTests
    {
        [Fact]
        public void SuggestMeals_NoIngredients_ReturnsEmpty()
        {
            var matches = MealSuggestionService.SuggestMeals(new List<string>());
            Assert.Empty(matches);
        }

        [Fact]
        public void SuggestMeals_ExactMatch_ReturnsFullMatchFirst()
        {
            var matches = MealSuggestionService.SuggestMeals(new[] { "eggs", "bread", "butter" });

            Assert.NotEmpty(matches);
            Assert.Equal("Eggs on Toast", matches[0].Recipe.Name);
            Assert.Equal(1.0, matches[0].MatchFraction);
        }

        [Fact]
        public void SuggestMeals_IsCaseAndWhitespaceInsensitive()
        {
            var matches = MealSuggestionService.SuggestMeals(new[] { " EGGS ", "Bread", "butter" });

            Assert.Contains(matches, m => m.Recipe.Name == "Eggs on Toast");
        }

        [Fact]
        public void SuggestMeals_BelowThreshold_IsExcluded()
        {
            // "Chicken and Rice" needs chicken, rice, onion - having only rice is a third.
            var matches = MealSuggestionService.SuggestMeals(new[] { "rice" }, minMatchFraction: 0.5);

            Assert.DoesNotContain(matches, m => m.Recipe.Name == "Chicken and Rice");
        }

        [Fact]
        public void SuggestMeals_OrdersByMatchFractionDescending()
        {
            var matches = MealSuggestionService.SuggestMeals(new[] { "rice", "beans", "onion", "tomato" });

            for (var i = 1; i < matches.Count; i++)
            {
                Assert.True(matches[i - 1].MatchFraction >= matches[i].MatchFraction);
            }
        }

        [Fact]
        public void GetWeeklyMenu_ReturnsSevenDaysCoveringMondayToSunday()
        {
            var menu = MealSuggestionService.GetWeeklyMenu(new DateTime(2026, 3, 18));

            Assert.Equal(7, menu.Count);
            Assert.Equal(new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
            }, menu.Select(d => d.Day));
        }

        [Fact]
        public void GetWeeklyMenu_IsDeterministicForSameWeek()
        {
            var menuA = MealSuggestionService.GetWeeklyMenu(new DateTime(2026, 3, 16));
            var menuB = MealSuggestionService.GetWeeklyMenu(new DateTime(2026, 3, 20));

            Assert.Equal(menuA.Select(d => d.Lunch.Name), menuB.Select(d => d.Lunch.Name));
        }
    }
}
