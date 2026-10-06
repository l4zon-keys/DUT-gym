using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    public enum MealSlot
    {
        Breakfast,
        Lunch,
        Dinner,
        Snack
    }

    public class StudentMealPlanEntry
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime WeekStartDate { get; set; }

        public DayOfWeek Day { get; set; }

        public MealSlot Slot { get; set; }

        [Required]
        [StringLength(80)]
        public string MealName { get; set; } = null!;

        [StringLength(300)]
        public string? Notes { get; set; }

        public int CaloriesKcal { get; set; }

        public int ProteinG { get; set; }

        public int CarbsG { get; set; }

        public int FatG { get; set; }

        public decimal EstimatedCostRand { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
