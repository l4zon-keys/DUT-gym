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

        [Required(ErrorMessage = "Please enter a location.")]
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

        [StringLength(260)]
        public string? ImagePath { get; set; }

        [StringLength(260)]
        public string? DamagePhotoPath { get; set; }

        // Procurement/asset details - not required at registration since older
        // equipment may not have this on record, but captured going forward so
        // staff have real data (brand reliability, useful life, supplier) to
        // base future purchasing decisions on.
        [StringLength(100)]
        public string? Brand { get; set; }

        [Display(Name = "Bought From")]
        [StringLength(150)]
        public string? Supplier { get; set; }

        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "Purchase Price (R)")]
        [Range(0, 10000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PurchasePrice { get; set; }

        [Display(Name = "Warranty Expiry")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyExpiryDate { get; set; }

        [Display(Name = "Insurance Provider")]
        [StringLength(150)]
        public string? InsuranceProvider { get; set; }

        [Display(Name = "Insurance Policy Number")]
        [StringLength(100)]
        public string? InsurancePolicyNumber { get; set; }

        [Display(Name = "Insurance Expiry")]
        [DataType(DataType.Date)]
        public DateTime? InsuranceExpiryDate { get; set; }
    }
}
