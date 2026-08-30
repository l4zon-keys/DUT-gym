using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A monthly progress entry against a FitnessGoal (PB-12).
    public class ProgressLog
    {
        public int Id { get; set; }

        [Required]
        public int FitnessGoalId { get; set; }

        [ForeignKey(nameof(FitnessGoalId))]
        public FitnessGoal? FitnessGoal { get; set; }

        [Range(20, 300)]
        public decimal? WeightKg { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    }
}
