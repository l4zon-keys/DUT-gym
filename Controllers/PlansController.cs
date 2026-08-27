using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    public class PlansController : AppControllerBase
    {
        public PlansController(MyDbContext db) : base(db)
        {
        }

        // Student/staff-facing browse list (PB-15).
        public async Task<IActionResult> Index()
        {
            var (_, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            var plans = await Db.MembershipPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();
            return View(plans);
        }

        // --- Staff CRUD (PB-16 subset) ---------------------------------

        public async Task<IActionResult> Manage()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var plans = await Db.MembershipPlans.OrderBy(p => p.Name).ToListAsync();
            return View(plans);
        }

        public IActionResult Create()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;
            return View(new MembershipPlan());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MembershipPlan plan)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid) return View(plan);

            Db.MembershipPlans.Add(plan);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Membership plan created.";
            return RedirectToAction(nameof(Manage));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var plan = await Db.MembershipPlans.FindAsync(id);
            if (plan == null) return NotFound();
            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MembershipPlan plan)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;
            if (id != plan.Id) return NotFound();

            if (!ModelState.IsValid) return View(plan);

            Db.MembershipPlans.Update(plan);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Membership plan updated.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var plan = await Db.MembershipPlans.FindAsync(id);
            if (plan == null) return NotFound();

            plan.IsActive = !plan.IsActive;
            await Db.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }
    }
}
