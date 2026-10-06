using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Rule-based meal/diet suggester (sprint 1 feedback) - no external AI API,
    // matches ingredients a student has against a small seeded recipe list.
    public class MealsController : AppControllerBase
    {
        public MealsController(MyDbContext db) : base(db)
        {
        }

        public async Task<IActionResult> Index()
        {
            var (user, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            await PopulatePlannerAsync(user!);
            await PopulateNutritionAsync(user!);
            return View(new List<MealMatch>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suggest(string? ingredients)
        {
            var (user, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var list = (ingredients ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ViewBag.Ingredients = ingredients;
            await PopulatePlannerAsync(user!);
            await PopulateNutritionAsync(user!);

            var matches = MealSuggestionService.SuggestMeals(list);
            return View("Index", matches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSuggestion(string recipeName, DayOfWeek day, MealSlot slot, string? ingredients)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var recipe = MealSuggestionService.Recipes.FirstOrDefault(r => r.Name == recipeName);
            if (recipe == null)
            {
                TempData["Error"] = "That meal suggestion could not be found.";
                return RedirectToAction(nameof(Index));
            }

            await UpsertMealAsync(student!.Id, day, slot, recipe.Name, recipe.Instructions, recipe.CaloriesKcal, recipe.ProteinG, recipe.CarbsG, recipe.FatG, recipe.EstimatedCostRand);
            TempData["Success"] = $"{recipe.Name} added to your {day} {slot}.";

            return string.IsNullOrWhiteSpace(ingredients)
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(IndexWithSearch), new { ingredients });
        }

        public async Task<IActionResult> IndexWithSearch(string? ingredients)
        {
            var (user, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var list = (ingredients ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ViewBag.Ingredients = ingredients;
            await PopulatePlannerAsync(user!);
            await PopulateNutritionAsync(user!);
            return View("Index", MealSuggestionService.SuggestMeals(list));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddManual(DayOfWeek day, MealSlot slot, string mealName, string? notes, int caloriesKcal, int proteinG, int carbsG, int fatG, decimal estimatedCostRand)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            if (string.IsNullOrWhiteSpace(mealName))
            {
                TempData["Error"] = "Enter a meal name before adding it to your plan.";
                return RedirectToAction(nameof(Index));
            }

            await UpsertMealAsync(student!.Id, day, slot, mealName.Trim(), notes, caloriesKcal, proteinG, carbsG, fatG, estimatedCostRand);
            TempData["Success"] = $"{mealName.Trim()} saved to your {day} {slot}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEntry(int id, string mealName, string? notes, int caloriesKcal, int proteinG, int carbsG, int fatG, decimal estimatedCostRand)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var entry = await Db.StudentMealPlanEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == student!.Id);
            if (entry == null) return NotFound();

            if (string.IsNullOrWhiteSpace(mealName))
            {
                TempData["Error"] = "Meal name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            entry.MealName = mealName.Trim();
            entry.Notes = notes;
            entry.CaloriesKcal = Math.Max(0, caloriesKcal);
            entry.ProteinG = Math.Max(0, proteinG);
            entry.CarbsG = Math.Max(0, carbsG);
            entry.FatG = Math.Max(0, fatG);
            entry.EstimatedCostRand = Math.Max(0, estimatedCostRand);
            await Db.SaveChangesAsync();

            TempData["Success"] = "Meal updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var entry = await Db.StudentMealPlanEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == student!.Id);
            if (entry != null)
            {
                Db.StudentMealPlanEntries.Remove(entry);
                await Db.SaveChangesAsync();
                TempData["Success"] = "Meal removed from your plan.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task UpsertMealAsync(int userId, DayOfWeek day, MealSlot slot, string mealName, string? notes, int caloriesKcal, int proteinG, int carbsG, int fatG, decimal estimatedCostRand)
        {
            var weekStart = GetCurrentWeekStart();
            var entry = await Db.StudentMealPlanEntries.FirstOrDefaultAsync(e =>
                e.UserId == userId && e.WeekStartDate == weekStart && e.Day == day && e.Slot == slot);

            if (entry == null)
            {
                entry = new StudentMealPlanEntry
                {
                    UserId = userId,
                    WeekStartDate = weekStart,
                    Day = day,
                    Slot = slot
                };
                Db.StudentMealPlanEntries.Add(entry);
            }

            entry.MealName = mealName;
            entry.Notes = notes;
            entry.CaloriesKcal = Math.Max(0, caloriesKcal);
            entry.ProteinG = Math.Max(0, proteinG);
            entry.CarbsG = Math.Max(0, carbsG);
            entry.FatG = Math.Max(0, fatG);
            entry.EstimatedCostRand = Math.Max(0, estimatedCostRand);
            await Db.SaveChangesAsync();
        }

        private async Task PopulatePlannerAsync(User student)
        {
            var weekStart = GetCurrentWeekStart();
            ViewBag.WeekStart = weekStart;
            ViewBag.WeekEnd = weekStart.AddDays(6);
            ViewBag.WeeklyMenu = MealSuggestionService.GetWeeklyMenu();
            ViewBag.MyMealPlan = await Db.StudentMealPlanEntries
                .Where(e => e.UserId == student.Id && e.WeekStartDate == weekStart)
                .OrderBy(e => e.Day)
                .ThenBy(e => e.Slot)
                .ToListAsync();
        }

        private static DateTime GetCurrentWeekStart()
        {
            var today = DateTime.UtcNow.Date;
            var diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return today.AddDays(-diff);
        }

        // Personal calorie/macro estimate + goal-tailored tips (PB-18). Degrades
        // gracefully to generic tips and no estimate if the student hasn't set a
        // fitness goal yet.
        private async Task PopulateNutritionAsync(User user)
        {
            var goal = await Db.FitnessGoals
                .Where(g => g.UserId == user.Id)
                .OrderByDescending(g => g.CreatedAt)
                .FirstOrDefaultAsync();

            if (goal?.StartingWeightKg != null)
            {
                ViewBag.CalorieEstimate = NutritionService.BuildEstimate(
                    goal.StartingWeightKg.Value, user.Gender, goal.GoalType, goal.ActivityLevel);
                ViewBag.Tips = NutritionService.GetTipsForGoal(goal.GoalType);
            }
            else
            {
                ViewBag.Tips = NutritionService.GetTipsForGoal(null);
            }
        }
    }
}
