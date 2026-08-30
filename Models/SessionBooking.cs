using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A student's booking of an admin-managed Session slot (PB-15/16 extension).
    public class SessionBooking
    {
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [ForeignKey(nameof(SessionId))]
        public Session? Session { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public bool Cancelled { get; set; }

        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
    }
}
