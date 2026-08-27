using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Staff-only CRUD (PB-16 subset). Scaffolding ahead of the session
    // booking engine (PB-9-11) which will consume these venues later.
    public class VenuesController : AppControllerBase
    {
        public VenuesController(MyDbContext db) : base(db)
        {
        }

        public async Task<IActionResult> Manage()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var venues = await Db.Venues.OrderBy(v => v.Name).ToListAsync();
            return View(venues);
        }

        public IActionResult Create()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;
            return View(new Venue());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid) return View(venue);

            Db.Venues.Add(venue);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Venue created.";
            return RedirectToAction(nameof(Manage));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var venue = await Db.Venues.FindAsync(id);
            if (venue == null) return NotFound();
            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Venue venue)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;
            if (id != venue.Id) return NotFound();

            if (!ModelState.IsValid) return View(venue);

            Db.Venues.Update(venue);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Venue updated.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var venue = await Db.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            venue.IsActive = !venue.IsActive;
            await Db.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }
    }
}
