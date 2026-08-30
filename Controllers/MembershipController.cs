using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    public class MembershipController : AppControllerBase
    {
        private static readonly string[] AllowedProofExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxProofFileBytes = 5 * 1024 * 1024;

        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;

        public MembershipController(MyDbContext db, IWebHostEnvironment environment, IConfiguration configuration) : base(db)
        {
            this.environment = environment;
            this.configuration = configuration;
        }

        // --- Apply (PB-3) -----------------------------------------------

        public async Task<IActionResult> Apply()
        {
            var (user, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var existing = await Db.Memberships
                .Where(m => m.UserId == user!.Id && (m.Status == MembershipStatus.Pending || m.Status == MembershipStatus.Active))
                .FirstOrDefaultAsync();
            if (existing != null)
            {
                return RedirectToAction(nameof(Status));
            }

            await PopulatePlans();
            PopulateTrainerOptions();
            return View(new Membership());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(Membership model, IFormFile registrationProofFile)
        {
            var (user, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            // Fields the student must not be able to set directly.
            ModelState.Remove(nameof(Membership.UserId));
            ModelState.Remove(nameof(Membership.Status));

            var plan = await Db.MembershipPlans.FindAsync(model.PlanId);
            if (plan == null || !plan.IsActive)
            {
                ModelState.AddModelError(nameof(model.PlanId), "Please select a valid membership plan.");
            }

            if (!model.MedicalConsentAccepted)
            {
                ModelState.AddModelError(nameof(model.MedicalConsentAccepted), "You must accept the medical indemnity consent to apply.");
            }

            string? extension = null;
            if (registrationProofFile == null || registrationProofFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please upload proof of registration (e.g. your DUT registration letter or student card).");
            }
            else
            {
                extension = Path.GetExtension(registrationProofFile.FileName).ToLowerInvariant();
                if (!AllowedProofExtensions.Contains(extension))
                {
                    ModelState.AddModelError(string.Empty, "Proof of registration must be a PDF, JPG, or PNG file.");
                }
                else if (registrationProofFile.Length > MaxProofFileBytes)
                {
                    ModelState.AddModelError(string.Empty, "Proof of registration file is too large (5MB max).");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulatePlans();
                PopulateTrainerOptions();
                return View(model);
            }

            model.UserId = user!.Id;
            model.Status = MembershipStatus.Pending;
            model.AppliedAt = DateTime.UtcNow;

            Db.Memberships.Add(model);
            await Db.SaveChangesAsync();

            var relativeDir = Path.Combine("uploads", "registration", model.Id.ToString());
            var absoluteDir = Path.Combine(environment.WebRootPath, relativeDir);
            Directory.CreateDirectory(absoluteDir);

            var generatedFileName = $"{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(absoluteDir, generatedFileName);
            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await registrationProofFile!.CopyToAsync(stream);
            }

            model.ProofOfRegistrationFilePath = Path.Combine(relativeDir, generatedFileName).Replace('\\', '/');
            await Db.SaveChangesAsync();

            return RedirectToAction(nameof(Pay), new { id = model.Id });
        }

        private async Task PopulatePlans()
        {
            var plans = await Db.MembershipPlans.Where(p => p.IsActive).OrderBy(p => p.Price).ToListAsync();
            ViewBag.Plans = plans.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Name} - R{p.Price:0.00} ({p.DurationMonths} month(s))"
            });
        }

        private void PopulateTrainerOptions()
        {
            ViewBag.TrainerOptions = Enum.GetValues<PersonalTrainerOption>().Select(o => new SelectListItem
            {
                Value = o.ToString(),
                Text = PersonalTrainerPricing.GetLabel(o)
            });
        }

        // --- Pay (PB-4) -----------------------------------------------------
        // No real payment processor is integrated here (see Services/ReferenceGenerator.cs
        // and the PaymentMethod/PaymentStatus enums for the design). Card/Eft/MobileMoney
        // settle immediately since there's no real processor to wait on; Cash stays
        // Pending until an admin confirms the money was physically received
        // (AdminController.ReviewMembership).

        public async Task<IActionResult> Pay(int id)
        {
            var (user, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships.Include(m => m.Plan).FirstOrDefaultAsync(m => m.Id == id);
            if (membership == null || membership.UserId != user!.Id) return NotFound();
            if (membership.Status != MembershipStatus.Pending) return RedirectToAction(nameof(Status));

            ViewBag.GymBanking = configuration.GetSection("GymBanking");
            return View(membership);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id, PaymentMethod method)
        {
            var (user, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships.Include(m => m.Plan).FirstOrDefaultAsync(m => m.Id == id);
            if (membership == null || membership.UserId != user!.Id) return NotFound();
            if (membership.Status != MembershipStatus.Pending) return RedirectToAction(nameof(Status));

            var isCash = method == PaymentMethod.Cash;

            var payment = new Payment
            {
                MembershipId = membership.Id,
                Method = method,
                Amount = membership.TotalCost,
                Reference = ReferenceGenerator.Generate("PAY"),
                Status = isCash ? PaymentStatus.Pending : PaymentStatus.Paid,
                PaidAt = isCash ? null : DateTime.UtcNow
            };
            Db.Payments.Add(payment);

            if (!isCash)
            {
                membership.Status = MembershipStatus.Active;
                membership.StartDate = DateTime.UtcNow.Date;
                membership.ExpiryDate = membership.StartDate.Value.AddMonths(membership.Plan!.DurationMonths);
            }

            await Db.SaveChangesAsync();

            TempData["Success"] = isCash
                ? "Your cash payment has been recorded - pay at the front desk. Your membership activates once staff confirm receipt."
                : "Payment confirmed - your membership is now active.";
            return RedirectToAction(nameof(Status));
        }

        // --- Status & receipt (PB-6) ---------------------------------------

        public async Task<IActionResult> Status()
        {
            var (user, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships
                .Include(m => m.Plan)
                .Include(m => m.Payments)
                .Where(m => m.UserId == user!.Id)
                .OrderByDescending(m => m.AppliedAt)
                .FirstOrDefaultAsync();

            return View(membership);
        }

        public async Task<IActionResult> Receipt(int id)
        {
            var (user, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships
                .Include(m => m.Plan)
                .Include(m => m.User)
                .Include(m => m.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (membership == null) return NotFound();
            if (user!.Role != EmailRoleHelper.StaffRole && membership.UserId != user.Id) return StatusCode(403);
            if (membership.Status != MembershipStatus.Active) return RedirectToAction(nameof(Status));

            return View(membership);
        }
    }
}
