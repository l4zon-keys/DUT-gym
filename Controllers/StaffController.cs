using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    public class StaffController : AppControllerBase
    {
        private readonly GymCapacityService capacityService;

        public StaffController(MyDbContext db, GymCapacityService capacityService) : base(db)
        {
            this.capacityService = capacityService;
        }

        // --- Verification queue (PB-5, verification side) -----------------

        public async Task<IActionResult> PendingMemberships()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var pending = await Db.Memberships
                .Include(m => m.User)
                .Include(m => m.Plan)
                .Include(m => m.Payments)
                .Where(m => m.Status == MembershipStatus.Pending)
                .OrderBy(m => m.AppliedAt)
                .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewMembership(int id, bool approve, string? reason)
        {
            var (staff, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships
                .Include(m => m.Plan)
                .Include(m => m.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membership == null) return NotFound();
            if (membership.Status != MembershipStatus.Pending) return RedirectToAction(nameof(PendingMemberships));

            var latestPayment = membership.Payments.OrderByDescending(p => p.SubmittedAt).FirstOrDefault();

            if (approve && latestPayment == null)
            {
                TempData["Error"] = "This application has no payment submitted yet and cannot be approved.";
                return RedirectToAction(nameof(PendingMemberships));
            }

            if (approve)
            {
                membership.Status = MembershipStatus.Active;
                membership.StartDate = DateTime.UtcNow.Date;
                membership.ExpiryDate = membership.StartDate.Value.AddMonths(membership.Plan!.DurationMonths);

                if (latestPayment != null)
                {
                    latestPayment.Status = PaymentStatus.Verified;
                    latestPayment.VerifiedByUserId = staff!.Id;
                    latestPayment.VerifiedAt = DateTime.UtcNow;
                }
            }
            else
            {
                membership.Status = MembershipStatus.Rejected;
                membership.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Not specified." : reason;

                if (latestPayment != null)
                {
                    latestPayment.Status = PaymentStatus.Rejected;
                    latestPayment.VerifiedByUserId = staff!.Id;
                    latestPayment.VerifiedAt = DateTime.UtcNow;
                }
            }

            membership.ReviewedByUserId = staff!.Id;
            membership.ReviewedAt = DateTime.UtcNow;

            await Db.SaveChangesAsync();
            TempData["Success"] = approve ? "Membership approved." : "Membership rejected.";
            return RedirectToAction(nameof(PendingMemberships));
        }

        // --- Check-in / check-out (PB-5, desk side) ------------------------

        public async Task<IActionResult> CheckIn(string? q)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            ViewBag.Query = q;

            if (string.IsNullOrWhiteSpace(q))
            {
                return View(new List<User>());
            }

            var term = q.Trim();
            var results = await Db.Users
                .Where(u => u.Role == EmailRoleHelper.StudentRole
                    && (u.StudentNumber.Contains(term) || u.Email.Contains(term)))
                .Take(20)
                .ToListAsync();

            return View(results);
        }

        public async Task<IActionResult> CheckInDetail(int userId)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var student = await Db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Role == EmailRoleHelper.StudentRole);
            if (student == null) return NotFound();

            var membership = await Db.Memberships
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.AppliedAt)
                .FirstOrDefaultAsync();

            var openCheckIn = await Db.CheckIns
                .Where(c => c.UserId == userId && c.CheckOutTime == null)
                .FirstOrDefaultAsync();

            ViewBag.Membership = membership;
            ViewBag.OpenCheckIn = openCheckIn;

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInStudent(int userId)
        {
            var (staff, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var membership = await Db.Memberships
                .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
                .OrderByDescending(m => m.ExpiryDate)
                .FirstOrDefaultAsync();

            if (!MembershipEligibility.CanCheckIn(membership, DateTime.UtcNow))
            {
                TempData["Error"] = "This student does not have an active, paid-up membership.";
                return RedirectToAction(nameof(CheckInDetail), new { userId });
            }

            var alreadyIn = await Db.CheckIns.AnyAsync(c => c.UserId == userId && c.CheckOutTime == null);
            if (!alreadyIn)
            {
                Db.CheckIns.Add(new CheckIn
                {
                    UserId = userId,
                    CheckedInByUserId = staff!.Id
                });
                await Db.SaveChangesAsync();
                TempData["Success"] = "Student checked in.";
            }

            return RedirectToAction(nameof(CheckInDetail), new { userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutStudent(int userId)
        {
            var (staff, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var openCheckIn = await Db.CheckIns.FirstOrDefaultAsync(c => c.UserId == userId && c.CheckOutTime == null);
            if (openCheckIn != null)
            {
                openCheckIn.CheckOutTime = DateTime.UtcNow;
                openCheckIn.CheckedOutByUserId = staff!.Id;
                await Db.SaveChangesAsync();
                TempData["Success"] = "Student checked out.";
            }

            return RedirectToAction(nameof(CheckInDetail), new { userId });
        }

        // --- Live capacity (PB-7) ------------------------------------------

        public async Task<IActionResult> Capacity()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var status = await capacityService.GetCurrentStatusAsync();
            return View(status);
        }
    }
}
