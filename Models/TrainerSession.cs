using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A scheduled PT appointment between an accepted trainer/student pair (PB-10).
    public class TrainerSession
    {
        public int Id { get; set; }

        [Required]
        public int TrainerUserId { get; set; }

        [ForeignKey(nameof(TrainerUserId))]
        public User? Trainer { get; set; }

        [Required]
        public int StudentUserId { get; set; }

        [ForeignKey(nameof(StudentUserId))]
        public User? Student { get; set; }

        [Required]
        public DateTime ScheduledAt { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }

        // Set once the 24h-ahead reminder email has gone out, so the background
        // service doesn't send it twice.
        public bool ReminderSent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
