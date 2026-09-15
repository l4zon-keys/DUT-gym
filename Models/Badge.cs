using System.ComponentModel.DataAnnotations;

namespace LoginFormASPCore6.Models
{
    // Catalog of earnable badges ("Early Bird", "5-Day Streak", "Zumba Fanatic").
    // Seeded at startup - see Program.cs.
    public class Badge
    {
        public int Id { get; set; }

        [Required]
        [StringLength(60)]
        public string Name { get; set; } = null!;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(40)]
        public string IconClass { get; set; } = "bi-award";

        // Stable machine key used by GamificationService.AwardBadgeIfEligibleAsync.
        [Required]
        [StringLength(40)]
        public string Code { get; set; } = null!;
    }
}
