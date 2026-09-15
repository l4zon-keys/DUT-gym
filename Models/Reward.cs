using System.ComponentModel.DataAnnotations;

namespace LoginFormASPCore6.Models
{
    // Catalog of XP-unlocked rewards (discount codes, certificates, a free PT
    // session, gym merch). Display-only in this app - earning one shows it as an
    // achievement, it does not generate a redeemable code or trigger fulfillment.
    // Seeded at startup - see Program.cs.
    public class Reward
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Name { get; set; } = null!;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(40)]
        public string IconClass { get; set; } = "bi-gift";

        public int RequiredXp { get; set; }
    }
}
