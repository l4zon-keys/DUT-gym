using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Desk staff: check-in/check-out and capacity only. Application review, venues,
    // sessions, and active-member management live in AdminController.
    public class StaffController : AppControllerBase
    {
        private readonly GymCapacityService capacityService;
        private readonly GamificationService gamificationService;
        private readonly AttendanceStreakService streakService;
        private readonly PredictiveCapacityService predictiveService;

        public StaffController(MyDbContext db, GymCapacityService capacityService, GamificationService gamificationService, AttendanceStreakService streakService, PredictiveCapacityService predictiveService) : base(db)
        {
            this.capacityService = capacityService;
            this.gamificationService = gamificationService;
            this.streakService = streakService;
            this.predictiveService = predictiveService;
        }

        // --- Check-in / check-out (PB-5, desk side) ------------------------

        public async Task<IActionResult> CheckIn(string? q)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            ViewBag.Query = q;

            IQueryable<User> query = Db.Users.Where(u => u.Role == EmailRoleHelper.StudentRole);

            if (string.IsNullOrWhiteSpace(q))
            {
                // No search yet: show everyone with an active membership so staff isn't
                // stuck typing a name/number blind.
                ViewBag.ShowingActiveList = true;
                query = query.Where(u => Db.Memberships.Any(m => m.UserId == u.Id && m.Status == MembershipStatus.Active));
            }
            else
            {
                ViewBag.ShowingActiveList = false;
                var term = q.Trim();
                query = query.Where(u => u.StudentNumber.Contains(term) || u.Email.Contains(term));
            }

            var results = await query.OrderBy(u => u.EmpName).Take(50).ToListAsync();
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

            var recentCheckIns = await Db.CheckIns
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CheckInTime)
                .Take(10)
                .ToListAsync();

            ViewBag.Membership = membership;
            ViewBag.OpenCheckIn = openCheckIn;
            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.TotalVisits = await Db.CheckIns.CountAsync(c => c.UserId == userId);

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
                var checkIn = new CheckIn
                {
                    UserId = userId,
                    CheckedInByUserId = staff!.Id
                };
                Db.CheckIns.Add(checkIn);
                await Db.SaveChangesAsync();
                TempData["Success"] = "Student checked in.";

                await AwardCheckInGamificationAsync(userId, checkIn);
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
            ViewBag.BestTimeToGo = await predictiveService.GetBestTimeToGoTodayAsync();
            return View(status);
        }

        // --- Gamification hook (PB-17) --------------------------------------

        private async Task AwardCheckInGamificationAsync(int userId, CheckIn checkIn)
        {
            await gamificationService.AwardXpAsync(userId, gamificationService.CheckInPoints, XpReason.CheckIn, checkIn.Id);

            if (GamificationService.IsEligibleForEarlyBird(checkIn.CheckInTime, gamificationService.EarlyBirdCutoffHour))
            {
                await gamificationService.AwardBadgeIfEligibleAsync(userId, "early_bird");
            }

            var daysThisWeek = await streakService.GetDaysAttendedThisWeekAsync(userId);
            if (GamificationService.IsEligibleForFiveDayStreak(daysThisWeek, gamificationService.FiveDayStreakDays))
            {
                // Idempotency key: one streak-milestone award per ISO week.
                var weekKey = DateTime.UtcNow.Year * 100 + System.Globalization.ISOWeek.GetWeekOfYear(DateTime.UtcNow);
                var awarded = await gamificationService.AwardXpAsync(userId, gamificationService.StreakMilestonePoints, XpReason.StreakMilestone, weekKey);
                if (awarded)
                {
                    await gamificationService.AwardBadgeIfEligibleAsync(userId, "five_day_streak");
                }
            }

            await gamificationService.EvaluateAndAwardRewardsAsync(userId);
        }
    }
}
