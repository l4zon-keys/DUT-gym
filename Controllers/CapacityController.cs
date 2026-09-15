using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Smarter live capacity (PB-19): current status + predicted busyness, plus
    // gym-wide slot booking with a waitlist, separate from the existing
    // per-class SessionBooking.
    public class CapacityController : AppControllerBase
    {
        private readonly GymCapacityService capacityService;
        private readonly PredictiveCapacityService predictiveService;
        private readonly CapacitySlotService slotService;
        private readonly IConfiguration configuration;

        public CapacityController(MyDbContext db, GymCapacityService capacityService, PredictiveCapacityService predictiveService, CapacitySlotService slotService, IConfiguration configuration) : base(db)
        {
            this.capacityService = capacityService;
            this.predictiveService = predictiveService;
            this.slotService = slotService;
            this.configuration = configuration;
        }

        // --- Current + predicted status ------------------------------------

        public async Task<IActionResult> Index()
        {
            var (_, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            ViewBag.Status = await capacityService.GetCurrentStatusAsync();
            ViewBag.PeakHours = await predictiveService.GetTodaysPeakHoursAsync();
            ViewBag.BestTimeToGo = await predictiveService.GetBestTimeToGoTodayAsync();
            return View();
        }

        // --- Student: browse/book slots --------------------------------------

        public async Task<IActionResult> Slots()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var slots = await Db.CapacitySlots
                .Where(s => s.EndTime >= DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var bookedCounts = await Db.CapacitySlotBookings
                .Where(b => b.Status == CapacitySlotBookingStatus.Booked)
                .GroupBy(b => b.CapacitySlotId)
                .Select(g => new { SlotId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.SlotId, g => g.Count);

            var waitlistCounts = await Db.CapacitySlotBookings
                .Where(b => b.Status == CapacitySlotBookingStatus.Waitlisted)
                .GroupBy(b => b.CapacitySlotId)
                .Select(g => new { SlotId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.SlotId, g => g.Count);

            var myBookings = await Db.CapacitySlotBookings
                .Where(b => b.UserId == student!.Id && b.Status != CapacitySlotBookingStatus.Cancelled)
                .Select(b => b.CapacitySlotId)
                .ToListAsync();

            ViewBag.BookedCounts = bookedCounts;
            ViewBag.WaitlistCounts = waitlistCounts;
            ViewBag.MySlotIds = myBookings.ToHashSet();
            return View(slots);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookSlot(int slotId)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var alreadyBooked = await Db.CapacitySlotBookings.AnyAsync(b =>
                b.CapacitySlotId == slotId && b.UserId == student!.Id && b.Status != CapacitySlotBookingStatus.Cancelled);
            if (alreadyBooked)
            {
                TempData["Error"] = "You've already booked or joined the waitlist for this slot.";
                return RedirectToAction(nameof(Slots));
            }

            var (booked, waitlisted) = await slotService.BookOrWaitlistAsync(slotId, student!.Id);
            if (!booked && !waitlisted)
            {
                return NotFound();
            }

            TempData["Success"] = booked ? "Slot booked." : "Slot is full - you've been added to the waitlist.";
            return RedirectToAction(nameof(MySlotBookings));
        }

        public async Task<IActionResult> MySlotBookings()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var bookings = await Db.CapacitySlotBookings
                .Include(b => b.CapacitySlot)
                .Where(b => b.UserId == student!.Id && b.Status != CapacitySlotBookingStatus.Cancelled)
                .OrderBy(b => b.CapacitySlot!.StartTime)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSlotBooking(int id)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            await slotService.CancelBookingAsync(id, student!.Id);
            TempData["Success"] = "Booking cancelled.";
            return RedirectToAction(nameof(MySlotBookings));
        }

        // --- Admin: manage slots --------------------------------------------

        public async Task<IActionResult> ManageSlots()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var slots = await Db.CapacitySlots.OrderByDescending(s => s.StartTime).ToListAsync();
            return View(slots);
        }

        public IActionResult CreateSlot()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var defaultCapacity = configuration.GetValue<int?>("CapacitySlots:DefaultSlotCapacity") ?? 50;
            return View(new CapacitySlot { Capacity = defaultCapacity });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSlot(CapacitySlot slot)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            if (slot.EndTime <= slot.StartTime)
            {
                ModelState.AddModelError(nameof(CapacitySlot.EndTime), "End time must be after the start time.");
            }
            if (!ModelState.IsValid)
            {
                return View(slot);
            }

            Db.CapacitySlots.Add(slot);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Slot created.";
            return RedirectToAction(nameof(ManageSlots));
        }

        public async Task<IActionResult> EditSlot(int id)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var slot = await Db.CapacitySlots.FindAsync(id);
            if (slot == null) return NotFound();
            return View(slot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSlot(int id, CapacitySlot slot)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;
            if (id != slot.Id) return NotFound();

            if (slot.EndTime <= slot.StartTime)
            {
                ModelState.AddModelError(nameof(CapacitySlot.EndTime), "End time must be after the start time.");
            }
            if (!ModelState.IsValid)
            {
                return View(slot);
            }

            Db.CapacitySlots.Update(slot);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Slot updated.";
            return RedirectToAction(nameof(ManageSlots));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var slot = await Db.CapacitySlots.FindAsync(id);
            if (slot == null) return NotFound();

            Db.CapacitySlots.Remove(slot);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Slot deleted.";
            return RedirectToAction(nameof(ManageSlots));
        }
    }
}
