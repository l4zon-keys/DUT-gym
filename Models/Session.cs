using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a session title.")]
        [StringLength(80, MinimumLength = 2)]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Please select a venue.")]
        [DisplayName("Venue")]
        public int VenueId { get; set; }

        [ForeignKey(nameof(VenueId))]
        public Venue? Venue { get; set; }

        [Required]
        [DisplayName("Start Time")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required]
        [DisplayName("End Time")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        [Required]
        [Range(1, 500, ErrorMessage = "Capacity must be a positive number.")]
        public int Capacity { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}
