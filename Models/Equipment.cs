using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    // Gym equipment inventory + fault reporting (sprint 1 feedback: desk staff
    // need to log faulty equipment, mark it inactive, and request maintenance).
    public class Equipment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter an equipment name.")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = null!;

        [StringLength(100)]
        public string? Location { get; set; }

        public EquipmentStatus Status { get; set; } = EquipmentStatus.Active;

        public EquipmentSeverity? Severity { get; set; }

        public int? ReportedByUserId { get; set; }

        [ForeignKey(nameof(ReportedByUserId))]
        public User? ReportedByUser { get; set; }

        public DateTime? ReportedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}
