using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A reward earned by a user - unique per (UserId, RewardId). Display-only:
    // no redemption code, no redeemed-at/by fields.
    public class UserReward
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public int RewardId { get; set; }

        [ForeignKey(nameof(RewardId))]
        public Reward? Reward { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }
}
