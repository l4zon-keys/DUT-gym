using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A directed friend connection: UserId sent the request to FriendUserId.
    // Feeds the "friends" XP leaderboard. Unique per (UserId, FriendUserId).
    public class Friendship
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public int FriendUserId { get; set; }

        [ForeignKey(nameof(FriendUserId))]
        public User? FriendUser { get; set; }

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
