using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A student's reservation (or waitlist entry) for a CapacitySlot. No DB-level
    // uniqueness constraint - "already booked/waitlisted" is an app-level check,
    // matching SessionBooking's existing convention.
    public class CapacitySlotBooking
    {
        public int Id { get; set; }

        [Required]
        public int CapacitySlotId { get; set; }

        [ForeignKey(nameof(CapacitySlotId))]
        public CapacitySlot? CapacitySlot { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public CapacitySlotBookingStatus Status { get; set; } = CapacitySlotBookingStatus.Booked;

        // Only set while Status == Waitlisted - orders promotion (lowest first).
        public int? WaitlistPosition { get; set; }

        public DateTime BookedAt { get; set; } = DateTime.UtcNow;

        // Set when a waitlist-promotion email has been sent, so a promotion is
        // never notified twice.
        public DateTime? NotifiedAt { get; set; }
    }
}
