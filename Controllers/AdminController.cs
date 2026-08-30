using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Admin-only: application review, and viewing/editing active members.
    // Venues/Sessions management lives in their own controllers, also gated to Admin.
    public class AdminController : AppControllerBase
    {
        private readonly IEmailSender emailSender;
        private readonly AttendanceReportService reportService;

        public AdminController(MyDbContext db, IEmailSender emailSender, AttendanceReportService reportService) : base(db)
        {
            this.emailSender = emailSender;
            this.reportService = reportService;
        }

        // --- Trainer applications -------------------------------------------

        public async Task<IActionResult> PendingTrainers()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var pending = await Db.Users
                .Where(u => u.Role == EmailRoleHelper.TrainerRole && u.ApprovalStatus == ApprovalStatus.Pending)
                .OrderBy(u => u.EmpName)
                .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewTrainer(int id, bool approve)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var trainer = await Db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == EmailRoleHelper.TrainerRole);
            if (trainer == null) return NotFound();

            trainer.ApprovalStatus = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            await Db.SaveChangesAsync();

            await emailSender.SendAsync(trainer.Email,
                approve ? "Trainer application approved" : "Trainer application rejected",
                approve
                    ? "Your personal trainer application has been approved. You can now log in and access the trainer portal."
                    : "Your personal trainer application was not approved.");

            TempData["Success"] = approve ? "Trainer approved." : "Trainer application rejected.";
            return RedirectToAction(nameof(PendingTrainers));
        }

        // --- Staff applications ----------------------------------------------

        public async Task<IActionResult> PendingStaff()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var pending = await Db.Users
                .Where(u => u.Role == EmailRoleHelper.StaffRole && u.ApprovalStatus == ApprovalStatus.Pending)
                .OrderBy(u => u.EmpName)
                .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewStaff(int id, bool approve)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var staff = await Db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == EmailRoleHelper.StaffRole);
            if (staff == null) return NotFound();

            staff.ApprovalStatus = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            await Db.SaveChangesAsync();

            await emailSender.SendAsync(staff.Email,
                approve ? "Staff application approved" : "Staff application rejected",
                approve
                    ? "Your staff application has been approved. You can now log in and access the staff dashboard."
                    : "Your staff application was not approved.");

            TempData["Success"] = approve ? "Staff member approved." : "Staff application rejected.";
            return RedirectToAction(nameof(PendingStaff));
        }

        // --- Attendance & usage reports (PB-8) -------------------------------

        public async Task<IActionResult> Reports()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var report = await reportService.BuildReportAsync();
            return View(report);
        }

        // --- Application review (PB-5, verification side) ------------------

        public async Task<IActionResult> PendingMemberships()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var pending = await Db.Memberships
                .Include(m => m.User)
                .Include(m => m.Plan)
                .Include(m => m.Payments)
                .Where(m => m.Status == MembershipStatus.Pending)
                .OrderBy(m => m.AppliedAt)
                .ToListAsync();

            // Recently rejected, in case one was rejected by mistake and needs reopening.
            ViewBag.RecentlyRejected = await Db.Memberships
                .Include(m => m.User)
                .Include(m => m.Plan)
                .Where(m => m.Status == MembershipStatus.Rejected)
                .OrderByDescending(m => m.ReviewedAt)
                .Take(10)
                .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoRejection(int id)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships
                .Include(m => m.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membership == null) return NotFound();
            if (membership.Status != MembershipStatus.Rejected) return RedirectToAction(nameof(PendingMemberships));

            membership.Status = MembershipStatus.Pending;
            membership.RejectionReason = null;
            membership.ReviewedByUserId = null;
            membership.ReviewedAt = null;

            var latestPayment = membership.Payments.OrderByDescending(p => p.SubmittedAt).FirstOrDefault();
            if (latestPayment != null && latestPayment.Status == PaymentStatus.Failed)
            {
                latestPayment.Status = PaymentStatus.Pending;
                latestPayment.ConfirmedByUserId = null;
            }

            await Db.SaveChangesAsync();
            TempData["Success"] = "Application reopened for review.";
            return RedirectToAction(nameof(PendingMemberships));
        }

        // Confirming (approve=true) only makes sense for a Cash payment sitting
        // Pending - Card/Eft/MobileMoney settle instantly in Checkout() and never
        // reach this queue. Rejecting can happen either way (e.g. cash never shows
        // up, or the application itself is invalid).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewMembership(int id, bool approve, string? reason)
        {
            var (admin, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships
                .Include(m => m.Plan)
                .Include(m => m.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membership == null) return NotFound();
            if (membership.Status != MembershipStatus.Pending) return RedirectToAction(nameof(PendingMemberships));

            var latestPayment = membership.Payments.OrderByDescending(p => p.SubmittedAt).FirstOrDefault();

            if (approve && (latestPayment == null || latestPayment.Method != PaymentMethod.Cash || latestPayment.Status != PaymentStatus.Pending))
            {
                TempData["Error"] = "There's no cash payment awaiting confirmation for this application.";
                return RedirectToAction(nameof(PendingMemberships));
            }

            if (approve)
            {
                membership.Status = MembershipStatus.Active;
                membership.StartDate = DateTime.UtcNow.Date;
                membership.ExpiryDate = membership.StartDate.Value.AddMonths(membership.Plan!.DurationMonths);

                latestPayment!.Status = PaymentStatus.Paid;
                latestPayment.PaidAt = DateTime.UtcNow;
                latestPayment.ConfirmedByUserId = admin!.Id;
            }
            else
            {
                membership.Status = MembershipStatus.Rejected;
                membership.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Not specified." : reason;

                if (latestPayment != null && latestPayment.Status == PaymentStatus.Pending)
                {
                    latestPayment.Status = PaymentStatus.Failed;
                }
            }

            membership.ReviewedByUserId = admin!.Id;
            membership.ReviewedAt = DateTime.UtcNow;

            await Db.SaveChangesAsync();
            TempData["Success"] = approve ? "Cash payment confirmed - membership is now active." : "Membership rejected.";
            return RedirectToAction(nameof(PendingMemberships));
        }

        // --- Active members ------------------------------------------------

        public async Task<IActionResult> ActiveMembers(string? q)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            ViewBag.Query = q;

            var query = Db.Memberships
                .Include(m => m.User)
                .Include(m => m.Plan)
                .Where(m => m.Status == MembershipStatus.Active);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(m => m.User!.EmpName.Contains(term)
                    || m.User!.StudentNumber.Contains(term)
                    || m.User!.Email.Contains(term));
            }

            var members = await query.OrderBy(m => m.User!.EmpName).ToListAsync();
            return View(members);
        }

        public async Task<IActionResult> EditMembership(int id)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships.Include(m => m.User).Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membership == null) return NotFound();

            return View(membership);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMembership(int id, Membership model)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships.Include(m => m.User).Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membership == null) return NotFound();
            if (id != model.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Campus))
            {
                ModelState.AddModelError(nameof(model.Campus), "Please enter a campus.");
            }
            if (string.IsNullOrWhiteSpace(model.EmergencyContactName))
            {
                ModelState.AddModelError(nameof(model.EmergencyContactName), "Please enter an emergency contact name.");
            }
            if (string.IsNullOrWhiteSpace(model.EmergencyContactPhone))
            {
                ModelState.AddModelError(nameof(model.EmergencyContactPhone), "Please enter an emergency contact phone number.");
            }

            // Only membership-specific fields are editable here - the underlying account
            // (name/email/password) is left alone.
            if (!ModelState.IsValid)
            {
                model.User = membership.User;
                model.Plan = membership.Plan;
                return View(model);
            }

            membership.Campus = model.Campus;
            membership.EmergencyContactName = model.EmergencyContactName;
            membership.EmergencyContactPhone = model.EmergencyContactPhone;
            membership.MedicalConditions = model.MedicalConditions;
            membership.PersonalTrainerOption = model.PersonalTrainerOption;
            membership.StartDate = model.StartDate;
            membership.ExpiryDate = model.ExpiryDate;

            await Db.SaveChangesAsync();
            TempData["Success"] = "Membership details updated.";
            return RedirectToAction(nameof(ActiveMembers));
        }
    }
}
