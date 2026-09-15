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
            var (user, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            ViewBag.WeeklyMenu = MealSuggestionService.GetWeeklyMenu();
            await PopulateNutritionAsync(user!);
            return View(new List<MealMatch>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suggest(string? ingredients)
        {
            var (user, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            var list = (ingredients ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ViewBag.WeeklyMenu = MealSuggestionService.GetWeeklyMenu();
            ViewBag.Ingredients = ingredients;
            await PopulateNutritionAsync(user!);

            var matches = MealSuggestionService.SuggestMeals(list);
            return View("Index", matches);
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
