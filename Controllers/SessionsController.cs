using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Admin-only CRUD for the session catalogue (PB-16), plus student browse/book (PB-15).
    public class SessionsController : AppControllerBase
    {
        public SessionsController(MyDbContext db) : base(db)
        {
        }

        // Pure - testable without a DB.
        public static bool CanBook(int capacity, int currentBookingCount) => currentBookingCount < capacity;

        // --- Student browse/book ---------------------------------------------

        public async Task<IActionResult> Browse()
        {
            var (_, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var sessions = await Db.Sessions
                .Include(s => s.Venue)
                .Where(s => s.StartTime >= DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var bookingCounts = await Db.SessionBookings
                .Where(b => !b.Cancelled)
                .GroupBy(b => b.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.SessionId, g => g.Count);

            ViewBag.BookingCounts = bookingCounts;
            return View(sessions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int sessionId)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var session = await Db.Sessions.FindAsync(sessionId);
            if (session == null) return NotFound();

            var alreadyBooked = await Db.SessionBookings.AnyAsync(b => b.SessionId == sessionId && b.UserId == student!.Id && !b.Cancelled);
            if (alreadyBooked)
            {
                TempData["Error"] = "You've already booked this session.";
                return RedirectToAction(nameof(Browse));
            }

            var currentCount = await Db.SessionBookings.CountAsync(b => b.SessionId == sessionId && !b.Cancelled);
            if (!CanBook(session.Capacity, currentCount))
            {
                TempData["Error"] = "This session is fully booked.";
                return RedirectToAction(nameof(Browse));
            }

            Db.SessionBookings.Add(new SessionBooking { SessionId = sessionId, UserId = student!.Id });
            await Db.SaveChangesAsync();

            TempData["Success"] = "Session booked.";
            return RedirectToAction(nameof(MyBookings));
        }

        public async Task<IActionResult> MyBookings()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var bookings = await Db.SessionBookings
                .Include(b => b.Session).ThenInclude(s => s!.Venue)
                .Where(b => b.UserId == student!.Id && !b.Cancelled)
                .OrderBy(b => b.Session!.StartTime)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var booking = await Db.SessionBookings.FirstOrDefaultAsync(b => b.Id == id && b.UserId == student!.Id);
            if (booking == null) return NotFound();

            booking.Cancelled = true;
            await Db.SaveChangesAsync();

            return RedirectToAction(nameof(MyBookings));
        }

        // --- Admin CRUD --------------------------------------------------------

        public async Task<IActionResult> Manage()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var sessions = await Db.Sessions
                .Include(s => s.Venue)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return View(sessions);
        }

        public async Task<IActionResult> Create()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            await PopulateVenues();
            return View(new Session());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Session session)
        {
            var (_, redirect) = RequireAdmin();
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
            var (_, redirect) = RequireAdmin();
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
            var (_, redirect) = RequireAdmin();
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
            var (_, redirect) = RequireAdmin();
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
