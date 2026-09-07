using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoginFormASPCore6.Controllers
{
    // Rule-based meal/diet suggester (sprint 1 feedback) - no external AI API,
    // matches ingredients a student has against a small seeded recipe list.
    public class MealsController : AppControllerBase
    {
        public MealsController(MyDbContext db) : base(db)
        {
        }

        public IActionResult Index()
        {
            var (_, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            ViewBag.WeeklyMenu = MealSuggestionService.GetWeeklyMenu();
            return View(new List<MealMatch>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Suggest(string? ingredients)
        {
            var (_, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            var list = (ingredients ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ViewBag.WeeklyMenu = MealSuggestionService.GetWeeklyMenu();
            ViewBag.Ingredients = ingredients;

            var matches = MealSuggestionService.SuggestMeals(list);
            return View("Index", matches);
        }
    }
}
