using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LoginFormASPCore6.Models
{
    public class Venue
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a venue name.")]
        [StringLength(80, MinimumLength = 2)]
        public string Name { get; set; } = null!;

        [StringLength(150)]
        public string? Location { get; set; }

        [Required]
        [Range(1, 2000, ErrorMessage = "Capacity must be a positive number.")]
        public int Capacity { get; set; }

        [DisplayName("Active")]
        public bool IsActive { get; set; } = true;
    }
}
