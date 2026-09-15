using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // An admin-created challenge (e.g. "September Step Challenge") students opt into.
    public class Challenge
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a challenge title.")]
        [StringLength(80, MinimumLength = 2)]
        public string Title { get; set; } = null!;

        [StringLength(300)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select what this challenge measures.")]
        public ChallengeMetric Metric { get; set; }

        [Required]
        [Range(1, 1000000, ErrorMessage = "Enter a positive target.")]
        [DisplayName("Target")]
        public decimal TargetValue { get; set; }

        [Required]
        [DisplayName("Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [Required]
        [DisplayName("End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddMonths(1);

        [Required]
        public int CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public User? CreatedByUser { get; set; }
    }
}
