using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginFormASPCore6.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int MembershipId { get; set; }

        [ForeignKey(nameof(MembershipId))]
        public Membership? Membership { get; set; }

        public PaymentMethod Method { get; set; }

        [Range(0, 100000)]
        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(50)]
        public string? GatewayProvider { get; set; }

        [StringLength(100)]
        public string? GatewayReference { get; set; }

        // Relative path under wwwroot/uploads/proofs/ — never a client-supplied filename.
        [StringLength(260)]
        public string? ProofFilePath { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public int? VerifiedByUserId { get; set; }

        [ForeignKey(nameof(VerifiedByUserId))]
        public User? VerifiedByUser { get; set; }

        public DateTime? VerifiedAt { get; set; }
    }
}
