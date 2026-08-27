using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Staff-only CRUD (PB-16 subset). No booking engine yet (PB-9-11 is a later sprint) -
    // this just lets staff maintain the session catalogue ahead of that.
    public class SessionsController : AppControllerBase
    {
        public SessionsController(MyDbContext db) : base(db)
        {
        }

        public async Task<IActionResult> Manage()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var sessions = await Db.Sessions
                .Include(s => s.Venue)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return View(sessions);
        }

        public async Task<IActionResult> Create()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            await PopulateVenues();
            return View(new Session());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Session session)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            if (session.EndTime <= session.StartTime)
            {
                ModelState.AddModelError(nameof(session.EndTime), "End time must be after the start time.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateVenues();
                return View(session);
            }

            Db.Sessions.Add(session);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Session created.";
            return RedirectToAction(nameof(Manage));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var session = await Db.Sessions.FindAsync(id);
            if (session == null) return NotFound();

            await PopulateVenues();
            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Session session)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;
            if (id != session.Id) return NotFound();

            if (session.EndTime <= session.StartTime)
            {
                ModelState.AddModelError(nameof(session.EndTime), "End time must be after the start time.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateVenues();
                return View(session);
            }

            Db.Sessions.Update(session);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Session updated.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var session = await Db.Sessions.FindAsync(id);
            if (session == null) return NotFound();

            Db.Sessions.Remove(session);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Session deleted.";
            return RedirectToAction(nameof(Manage));
        }

        private async Task PopulateVenues()
        {
            var venues = await Db.Venues.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync();
            ViewBag.Venues = venues.Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Name });
        }
    }
}
