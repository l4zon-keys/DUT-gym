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

        [Required]
        [StringLength(20)]
        public string Reference { get; set; } = null!;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // When it actually settled - immediately for Card/Eft/MobileMoney, or when
        // an admin confirms a Cash payment was physically received.
        public DateTime? PaidAt { get; set; }

        public int? ConfirmedByUserId { get; set; }

        [ForeignKey(nameof(ConfirmedByUserId))]
        public User? ConfirmedByUser { get; set; }
    }
}
