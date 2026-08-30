using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A student's request to be taken on by a trainer (PB-9).
    public class TrainerRequest
    {
        public int Id { get; set; }

        [Required]
        public int StudentUserId { get; set; }

        [ForeignKey(nameof(StudentUserId))]
        public User? Student { get; set; }

        [Required]
        public int TrainerUserId { get; set; }

        [ForeignKey(nameof(TrainerUserId))]
        public User? Trainer { get; set; }

        [StringLength(300)]
        public string? Message { get; set; }

        public TrainerRequestStatus Status { get; set; } = TrainerRequestStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }
    }
}
