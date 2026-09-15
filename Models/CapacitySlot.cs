using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LoginFormASPCore6.Models
{
    // A bookable gym-wide time window (distinct from a class Session) - lets a
    // student reserve a spot to work out during a busy period, with a waitlist
    // once it's full. No VenueId: this is whole-gym entry capacity, not tied to
    // a specific bookable room.
    public class CapacitySlot
    {
        public int Id { get; set; }

        [Required]
        [DisplayName("Start Time")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required]
        [DisplayName("End Time")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        [Required]
        [Range(1, 2000, ErrorMessage = "Capacity must be a positive number.")]
        public int Capacity { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}
