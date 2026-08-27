using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LoginFormASPCore6.Models
{
    public class MembershipPlan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a plan name.")]
        [DisplayName("Plan Name")]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = null!;

        [DisplayName("Description")]
        [StringLength(300)]
        public string? Description { get; set; }

        [Required]
        [Range(0, 100000, ErrorMessage = "Price must be a positive amount.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 36, ErrorMessage = "Duration must be between 1 and 36 months.")]
        [DisplayName("Duration (Months)")]
        public int DurationMonths { get; set; }

        [DisplayName("Active")]
        public bool IsActive { get; set; } = true;
    }
}
