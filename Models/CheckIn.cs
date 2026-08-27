using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    public class CheckIn
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;

        public DateTime? CheckOutTime { get; set; }

        [Required]
        public int CheckedInByUserId { get; set; }

        [ForeignKey(nameof(CheckedInByUserId))]
        public User? CheckedInByUser { get; set; }

        public int? CheckedOutByUserId { get; set; }

        [ForeignKey(nameof(CheckedOutByUserId))]
        public User? CheckedOutByUser { get; set; }
    }
}
