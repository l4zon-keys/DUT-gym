namespace LoginFormASPCore6.Services
{
    public record Recipe(string Name, string[] Ingredients, decimal EstimatedCostRand, string Instructions,
        int CaloriesKcal, int ProteinG, int CarbsG, int FatG);

    public record MealMatch(Recipe Recipe, int MatchedCount, double MatchFraction);

    public record MenuDay(DayOfWeek Day, Recipe Breakfast, Recipe Lunch, Recipe Dinner);

    // A small, self-contained (no external API) affordable-meal suggester for
    // students, driven by ingredients they say they already have (sprint 1
    // feedback: diet/meal-prep assistance).
    public static class MealSuggestionService
    {
        public static readonly IReadOnlyList<Recipe> Recipes = new List<Recipe>
        {
            new("Eggs on Toast", new[] { "eggs", "bread", "butter" }, 18, "Fry the eggs, serve on buttered toast.", 350, 18, 30, 18),
            new("Peanut Butter Oats", new[] { "oats", "milk", "peanut butter" }, 15, "Cook oats in milk, stir in peanut butter.", 420, 15, 55, 16),
            new("Tuna Sandwich", new[] { "tuna", "bread", "mayonnaise" }, 22, "Mix tuna with mayonnaise, spread on bread.", 380, 25, 35, 15),
            new("Rice and Beans", new[] { "rice", "beans", "onion", "tomato" }, 25, "Cook rice; simmer beans with onion and tomato; serve together.", 450, 15, 85, 5),
            new("Chicken and Rice", new[] { "chicken", "rice", "onion" }, 45, "Fry chicken with onion, serve over cooked rice.", 550, 40, 60, 12),
            new("Mince and Potato Bake", new[] { "mince", "potato", "onion", "tomato" }, 55, "Brown mince with onion and tomato, layer with sliced potato, bake.", 600, 35, 50, 28),
            new("Vegetable Fried Rice", new[] { "rice", "eggs", "onion", "carrot" }, 28, "Fry rice with scrambled eggs, onion, and carrot.", 420, 14, 65, 12),
            new("Pasta with Tomato Sauce", new[] { "pasta", "tomato", "onion", "garlic" }, 24, "Boil pasta; simmer tomato, onion, and garlic into a sauce; combine.", 450, 13, 85, 7),
            new("Tuna Pasta Bake", new[] { "pasta", "tuna", "tomato", "cheese" }, 40, "Combine cooked pasta with tuna and tomato, top with cheese, bake.", 520, 32, 65, 14),
            new("Bean Stew", new[] { "beans", "tomato", "onion", "spinach" }, 30, "Simmer beans with tomato and onion, stir in spinach at the end.", 350, 18, 55, 5),
            new("Chicken Stir Fry", new[] { "chicken", "carrot", "onion", "spinach" }, 50, "Stir fry chicken with carrot, onion, and spinach.", 400, 42, 20, 16),
            new("Mince Spaghetti", new[] { "mince", "pasta", "tomato", "onion" }, 48, "Brown mince with onion and tomato, serve over cooked pasta.", 600, 34, 70, 20),
            new("Potato and Egg Hash", new[] { "potato", "eggs", "onion" }, 20, "Fry diced potato and onion, top with fried eggs.", 380, 16, 45, 15),
            new("Oats with Banana", new[] { "oats", "milk", "banana" }, 14, "Cook oats in milk, top with sliced banana.", 380, 12, 65, 8),
            new("Spinach and Cheese Toast", new[] { "bread", "spinach", "cheese" }, 20, "Wilt spinach, top toast with spinach and cheese, grill.", 320, 15, 30, 15),
            new("Chicken Soup", new[] { "chicken", "carrot", "onion", "potato" }, 42, "Simmer chicken with carrot, onion, and potato until tender.", 350, 30, 35, 8),
            new("Tomato and Bean Toast", new[] { "bread", "beans", "tomato" }, 16, "Warm beans with tomato, serve on toast.", 320, 14, 55, 5),
            new("Egg Fried Rice", new[] { "rice", "eggs", "onion" }, 20, "Fry cooked rice with scrambled eggs and onion.", 400, 14, 60, 12),
        };

        // Normalizes an ingredient name for matching (case/whitespace-insensitive).
        private static string Normalize(string ingredient) => ingredient.Trim().ToLowerInvariant();

        public static List<MealMatch> SuggestMeals(IEnumerable<string> haveIngredients, double minMatchFraction = 0.5)
        {
            var have = haveIngredients
                .Select(Normalize)
                .Where(i => i.Length > 0)
                .ToHashSet();

            if (have.Count == 0)
            {
                return new List<MealMatch>();
            }

            return Recipes
                .Select(r =>
                {
                    var matched = r.Ingredients.Count(i => have.Contains(Normalize(i)));
                    var fraction = (double)matched / r.Ingredients.Length;
                    return new MealMatch(r, matched, fraction);
                })
                .Where(m => m.MatchFraction >= minMatchFraction)
                .OrderByDescending(m => m.MatchFraction)
                .ThenByDescending(m => m.MatchedCount)
                .ToList();
        }

        // A deterministic Mon-Sun meal-prep menu, rotating by ISO week number so
        // it varies week to week without needing any stored state.
        public static List<MenuDay> GetWeeklyMenu(DateTime? asOfUtc = null)
        {
            var asOf = asOfUtc ?? DateTime.UtcNow;
            var weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(asOf);
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            var breakfasts = Recipes.Where(r => r.Ingredients.Contains("oats") || r.Ingredients.Contains("eggs") && r.Ingredients.Contains("bread")).ToList();
            var mains = Recipes.Where(r => !breakfasts.Contains(r)).ToList();

            return days.Select((day, i) => new MenuDay(
                day,
                breakfasts[(weekNumber + i) % breakfasts.Count],
                mains[(weekNumber + i * 2) % mains.Count],
                mains[(weekNumber + i * 2 + 1) % mains.Count]
            )).ToList();
        }
    }
}
