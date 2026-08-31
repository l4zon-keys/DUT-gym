using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    public class Membership
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required(ErrorMessage = "Please select a membership plan.")]
        [DisplayName("Membership Plan")]
        public int PlanId { get; set; }

        [ForeignKey(nameof(PlanId))]
        public MembershipPlan? Plan { get; set; }

        public MembershipStatus Status { get; set; } = MembershipStatus.Pending;

        [Required(ErrorMessage = "Please select your campus.")]
        [StringLength(80)]
        public string Campus { get; set; } = null!;

        [Required(ErrorMessage = "Please provide an emergency contact name.")]
        [DisplayName("Emergency Contact Name")]
        [StringLength(80)]
        public string EmergencyContactName { get; set; } = null!;

        [Required(ErrorMessage = "Please provide an emergency contact phone number.")]
        [DisplayName("Emergency Contact Phone")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
        [StringLength(20)]
        public string EmergencyContactPhone { get; set; } = null!;

        [DisplayName("Medical Conditions")]
        [StringLength(500)]
        public string? MedicalConditions { get; set; }

        // Enforced explicitly in MembershipController.Apply rather than via a DataAnnotations
        // attribute - [Range(typeof(bool), ...)] does not play well with jQuery unobtrusive
        // client-side validation on a checkbox and silently blocks submission.
        [DisplayName("I consent to the medical indemnity waiver")]
        public bool MedicalConsentAccepted { get; set; }

        [DisplayName("Personal Trainer")]
        public PersonalTrainerOption PersonalTrainerOption { get; set; } = PersonalTrainerOption.None;

        // Relative path under wwwroot/uploads/registration/ - never a client-supplied filename.
        [StringLength(260)]
        public string? ProofOfRegistrationFilePath { get; set; }

        // Quoted price before payment - plan price + trainer fee at CURRENT rates. Use this
        // while a membership is still Pending (nothing charged yet).
        [NotMapped]
        public decimal TotalCost => (Plan?.Price ?? 0) + PersonalTrainerPricing.Fees.GetValueOrDefault(PersonalTrainerOption);

        // The amount actually charged, from the verified payment record - immutable even if
        // plan prices change later. Falls back to the live quote only if nothing's been paid yet.
        [NotMapped]
        public decimal AmountPaid => Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => (decimal?)p.Amount)
            .FirstOrDefault() ?? TotalCost;

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int? ReviewedByUserId { get; set; }

        [ForeignKey(nameof(ReviewedByUserId))]
        public User? ReviewedByUser { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // Why the membership is Rejected or Deactivated - one shared field rather
        // than a separate reason column per negative status.
        [StringLength(300)]
        public string? StatusNote { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
