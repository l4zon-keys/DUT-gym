using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // Append-only XP ledger - a running total is never stored directly, it's
    // always summed from this table, so awards can't race or double-apply silently.
    public class XpEvent
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public int Points { get; set; }

        public XpReason Reason { get; set; }

        // The CheckIn/FitnessGoal/SessionBooking/Challenge row that triggered this
        // award, if any - used to guard against double-awarding the same event.
        public int? SourceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
