using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A student's fitness goal (PB-12) - one active goal per student.
    public class FitnessGoal
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required(ErrorMessage = "Please select a goal type.")]
        [DisplayName("Goal Type")]
        public GoalType GoalType { get; set; }

        [StringLength(200)]
        [DisplayName("Details")]
        public string? Description { get; set; }

        [DisplayName("Starting Weight (kg)")]
        [Range(20, 300)]
        public decimal? StartingWeightKg { get; set; }

        [DisplayName("Target Weight (kg)")]
        [Range(20, 300)]
        public decimal? TargetWeightKg { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProgressLog> ProgressLogs { get; set; } = new List<ProgressLog>();
    }
}
