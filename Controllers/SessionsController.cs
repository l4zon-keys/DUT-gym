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
                .Include(s => s.Instructor)
                .Where(s => s.StartTime >= DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var bookingCounts = await Db.SessionBookings
                .Where(b => !b.Cancelled)
                .GroupBy(b => b.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.SessionId, g => g.Count);

            ViewBag.BookingCounts = bookingCounts;
            ViewBag.SessionsByDay = sessions.GroupBy(s => s.StartTime.Date).OrderBy(g => g.Key).ToList();
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
                .Include(s => s.Instructor)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return View(sessions);
        }

        public async Task<IActionResult> Create()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            await PopulateVenues();
            await PopulateInstructors();
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
                await PopulateInstructors();
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
            await PopulateInstructors();
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
                await PopulateInstructors();
                return View(session);
            }

            Db.Sessions.Update(session);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Session updated.";
            return RedirectToAction(nameof(Manage));
        }

        // --- Recurring class schedule (aerobics/Zumba style weekly classes) -----

        public async Task<IActionResult> CreateRecurring()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            await PopulateVenues();
            await PopulateInstructors();
            return View(new RecurringSessionRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRecurring(RecurringSessionRequest request)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            if (request.EndTime <= request.StartTime)
            {
                ModelState.AddModelError(nameof(request.EndTime), "End time must be after the start time.");
            }
            if (request.Weekdays == null || request.Weekdays.Length == 0)
            {
                ModelState.AddModelError(nameof(request.Weekdays), "Select at least one weekday.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateVenues();
                await PopulateInstructors();
                return View(request);
            }

            var dates = GenerateOccurrenceDates(DateTime.UtcNow.Date, request.Weekdays!, request.WeeksToGenerate);
            foreach (var date in dates)
            {
                Db.Sessions.Add(new Session
                {
                    Title = request.Title,
                    VenueId = request.VenueId,
                    Category = request.Category,
                    InstructorUserId = request.InstructorUserId,
                    Capacity = request.Capacity,
                    Notes = request.Notes,
                    StartTime = date.Add(request.StartTime.TimeOfDay),
                    EndTime = date.Add(request.EndTime.TimeOfDay)
                });
            }
            await Db.SaveChangesAsync();

            TempData["Success"] = $"Created {dates.Count} session(s).";
            return RedirectToAction(nameof(Manage));
        }

        // Pure - future dates (starting the day after fromDateUtc, capped at
        // weeksToGenerate weeks out) that fall on one of the given weekdays.
        public static List<DateTime> GenerateOccurrenceDates(DateTime fromDateUtc, DayOfWeek[] weekdays, int weeksToGenerate)
        {
            var weekdaySet = weekdays.ToHashSet();
            var dates = new List<DateTime>();
            var cutoff = fromDateUtc.AddDays(weeksToGenerate * 7);
            for (var date = fromDateUtc.AddDays(1); date <= cutoff; date = date.AddDays(1))
            {
                if (weekdaySet.Contains(date.DayOfWeek))
                {
                    dates.Add(date);
                }
            }
            return dates;
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

        private async Task PopulateInstructors()
        {
            var instructors = await Db.Users
                .Where(u => u.Role == EmailRoleHelper.TrainerRole && u.ApprovalStatus == ApprovalStatus.Approved)
                .OrderBy(u => u.EmpName)
                .ToListAsync();
            ViewBag.Instructors = instructors.Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.EmpName });
        }
    }

    // Form model for generating a batch of weekly-recurring class sessions
    // (e.g. "Zumba every Tue/Thu 6pm for the next 8 weeks").
    public class RecurringSessionRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please enter a session title.")]
        [System.ComponentModel.DataAnnotations.StringLength(80, MinimumLength = 2)]
        public string Title { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a venue.")]
        public int VenueId { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(40)]
        public string? Category { get; set; }

        public int? InstructorUserId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public DateTime StartTime { get; set; } = DateTime.Today.AddHours(18);

        [System.ComponentModel.DataAnnotations.Required]
        public DateTime EndTime { get; set; } = DateTime.Today.AddHours(19);

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(1, 500)]
        public int Capacity { get; set; } = 20;

        [System.ComponentModel.DataAnnotations.StringLength(300)]
        public string? Notes { get; set; }

        public DayOfWeek[]? Weekdays { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, 12)]
        public int WeeksToGenerate { get; set; } = 8;
    }
}
