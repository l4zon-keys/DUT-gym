using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // A workout plan a trainer uploads for one of their assigned students (PB-11).
    public class WorkoutPlan
    {
        public int Id { get; set; }

        [Required]
        public int TrainerUserId { get; set; }

        [ForeignKey(nameof(TrainerUserId))]
        public User? Trainer { get; set; }

        [Required]
        public int StudentUserId { get; set; }

        [ForeignKey(nameof(StudentUserId))]
        public User? Student { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        [StringLength(300)]
        public string? Notes { get; set; }

        // Relative path under wwwroot/uploads/workoutplans/.
        [Required]
        [StringLength(260)]
        public string FilePath { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
