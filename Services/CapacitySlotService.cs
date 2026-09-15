using LoginFormASPCore6.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Services
{
    // Gym-wide slot booking + waitlist. Waitlist is modeled as a Status on the
    // same CapacitySlotBooking row (not a separate table), so promotion is a
    // single UPDATE rather than a delete+insert across two tables.
    public class CapacitySlotService
    {
        private readonly MyDbContext db;
        private readonly IEmailSender emailSender;

        public CapacitySlotService(MyDbContext db, IEmailSender emailSender)
        {
            this.db = db;
            this.emailSender = emailSender;
        }

        // Pure - testable without a DB. Mirrors SessionsController.CanBook.
        public static bool HasOpenSpot(int capacity, int bookedCount) => bookedCount < capacity;

        public static int NextWaitlistPosition(IEnumerable<int> existingPositions)
        {
            var positions = existingPositions.ToList();
            return positions.Count == 0 ? 1 : positions.Max() + 1;
        }

        public async Task<(bool Booked, bool Waitlisted)> BookOrWaitlistAsync(int slotId, int userId)
        {
            var slot = await db.CapacitySlots.FindAsync(slotId);
            if (slot == null) return (false, false);

            var bookedCount = await db.CapacitySlotBookings
                .CountAsync(b => b.CapacitySlotId == slotId && b.Status == CapacitySlotBookingStatus.Booked);

            if (HasOpenSpot(slot.Capacity, bookedCount))
            {
                db.CapacitySlotBookings.Add(new CapacitySlotBooking
                {
                    CapacitySlotId = slotId,
                    UserId = userId,
                    Status = CapacitySlotBookingStatus.Booked
                });
                await db.SaveChangesAsync();
                return (true, false);
            }

            var existingPositions = await db.CapacitySlotBookings
                .Where(b => b.CapacitySlotId == slotId && b.Status == CapacitySlotBookingStatus.Waitlisted)
                .Select(b => b.WaitlistPosition!.Value)
                .ToListAsync();

            db.CapacitySlotBookings.Add(new CapacitySlotBooking
            {
                CapacitySlotId = slotId,
                UserId = userId,
                Status = CapacitySlotBookingStatus.Waitlisted,
                WaitlistPosition = NextWaitlistPosition(existingPositions)
            });
            await db.SaveChangesAsync();
            return (false, true);
        }

        public async Task CancelBookingAsync(int bookingId, int userId)
        {
            var booking = await db.CapacitySlotBookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
            if (booking == null) return;

            var wasBooked = booking.Status == CapacitySlotBookingStatus.Booked;
            var slotId = booking.CapacitySlotId;
            booking.Status = CapacitySlotBookingStatus.Cancelled;
            booking.WaitlistPosition = null;
            await db.SaveChangesAsync();

            if (wasBooked)
            {
                await PromoteNextWaitlistedAsync(slotId);
            }
        }

        public async Task PromoteNextWaitlistedAsync(int slotId)
        {
            var next = await db.CapacitySlotBookings
                .Include(b => b.User)
                .Where(b => b.CapacitySlotId == slotId && b.Status == CapacitySlotBookingStatus.Waitlisted)
                .OrderBy(b => b.WaitlistPosition)
                .FirstOrDefaultAsync();
            if (next == null) return;

            var slot = await db.CapacitySlots.FindAsync(slotId);

            next.Status = CapacitySlotBookingStatus.Booked;
            next.WaitlistPosition = null;
            next.NotifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            if (slot != null && next.User != null)
            {
                await emailSender.SendAsync(next.User.Email, "A spot opened up",
                    $"Good news - a spot opened up for your waitlisted slot on {slot.StartTime:f}. You're now booked in.");
            }
        }

        public async Task<CapacityStatus> GetSlotOccupancyAsync(int slotId)
        {
            var slot = await db.CapacitySlots.FindAsync(slotId);
            var bookedCount = await db.CapacitySlotBookings
                .CountAsync(b => b.CapacitySlotId == slotId && b.Status == CapacitySlotBookingStatus.Booked);

            var capacity = slot?.Capacity ?? 0;
            return new CapacityStatus
            {
                CurrentOccupancy = bookedCount,
                Threshold = capacity,
                Level = GymCapacityService.CalculateLevel(bookedCount, capacity, 0.5, 0.8)
            };
        }
    }
}
