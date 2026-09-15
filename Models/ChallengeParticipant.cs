using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A student's self-reported progress toward a Challenge - unique per
    // (ChallengeId, UserId).
    public class ChallengeParticipant
    {
        public int Id { get; set; }

        [Required]
        public int ChallengeId { get; set; }

        [ForeignKey(nameof(ChallengeId))]
        public Challenge? Challenge { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public decimal CurrentValue { get; set; }

        public bool Completed { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
