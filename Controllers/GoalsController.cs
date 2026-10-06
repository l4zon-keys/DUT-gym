using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security;
using System.Text;

namespace LoginFormASPCore6.Controllers
{
    // Fitness goals, progress logging, attendance streaks, leaderboard, and
    // certificates (PB-12/13/14).
    public class GoalsController : AppControllerBase
    {
        private readonly AttendanceStreakService streakService;
        private readonly GamificationService gamificationService;

        public GoalsController(MyDbContext db, AttendanceStreakService streakService, GamificationService gamificationService) : base(db)
        {
            this.streakService = streakService;
            this.gamificationService = gamificationService;
        }

        public async Task<IActionResult> MyGoal()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var goal = await Db.FitnessGoals
                .Include(g => g.ProgressLogs)
                .Where(g => g.UserId == student!.Id)
                .OrderByDescending(g => g.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.Streak = await streakService.GetMonthlyStreakAsync(student!.Id);
            ViewBag.VisitsThisMonth = await streakService.GetVisitCountForMonthAsync(student.Id, DateTime.UtcNow);
            ViewBag.CertificateEligible = AttendanceStreakService.IsEligibleForCertificate((int)ViewBag.VisitsThisMonth);
            ViewBag.CertificateRequiredVisits = 10;
            ViewBag.DaysThisWeek = await streakService.GetDaysAttendedThisWeekAsync(student.Id);
            ViewBag.AverageVisitMinutes = await streakService.GetAverageVisitMinutesAsync(student.Id);
            ViewBag.TodayMinutes = await streakService.GetTodayMinutesAsync(student.Id);

            return View(goal);
        }

        public async Task<IActionResult> SetGoal()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            ViewBag.IsOnboarding = !await Db.FitnessGoals.AnyAsync(g => g.UserId == student!.Id);
            return View(new FitnessGoal());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetGoal(FitnessGoal model)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            ModelState.Remove(nameof(FitnessGoal.UserId));
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.UserId = student!.Id;
            model.CreatedAt = DateTime.UtcNow;
            Db.FitnessGoals.Add(model);
            await Db.SaveChangesAsync();

            await gamificationService.AwardXpAsync(student.Id, gamificationService.GoalSetPoints, XpReason.GoalSet, model.Id);
            await gamificationService.EvaluateAndAwardRewardsAsync(student.Id);

            TempData["Success"] = "Goal set.";
            return RedirectToAction(nameof(MyGoal));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogProgress(int goalId, decimal? weightKg, string? notes)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var goal = await Db.FitnessGoals.FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == student!.Id);
            if (goal == null) return NotFound();

            if (!ProgressLogRules.IsValidWeight(weightKg))
            {
                TempData["Error"] = "Enter a weight between 20 and 300 kg.";
                return RedirectToAction(nameof(MyGoal));
            }

            var today = DateTime.UtcNow.Date;
            var checkedInToday = await Db.CheckIns.AnyAsync(c =>
                c.UserId == student!.Id && c.CheckInTime >= today && c.CheckInTime < today.AddDays(1));
            if (!checkedInToday)
            {
                TempData["Error"] = "Check in at the gym today before logging progress.";
                return RedirectToAction(nameof(MyGoal));
            }

            var progressLog = new ProgressLog
            {
                FitnessGoalId = goalId,
                WeightKg = weightKg,
                Notes = notes
            };
            Db.ProgressLogs.Add(progressLog);
            await Db.SaveChangesAsync();

            await gamificationService.AwardXpAsync(student!.Id, gamificationService.ProgressLoggedPoints, XpReason.ProgressLogged, progressLog.Id);
            await gamificationService.EvaluateAndAwardRewardsAsync(student.Id);

            TempData["Success"] = ProgressLogRules.BuildProgressMessage(goal.GoalType, weightKg!.Value, goal.TargetWeightKg);
            return RedirectToAction(nameof(MyGoal));
        }

        public async Task<IActionResult> Leaderboard()
        {
            var (_, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            var leaderboard = await streakService.GetLeaderboardAsync(DateTime.UtcNow, topN: 10);
            return View(leaderboard);
        }

        public async Task<IActionResult> Certificate()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var visits = await streakService.GetVisitCountForMonthAsync(student!.Id, DateTime.UtcNow);
            if (!AttendanceStreakService.IsEligibleForCertificate(visits))
            {
                TempData["Error"] = "You haven't met this month's attendance target yet.";
                return RedirectToAction(nameof(MyGoal));
            }

            ViewBag.Visits = visits;
            ViewBag.Month = DateTime.UtcNow.ToString("MMMM yyyy");
            ViewBag.RequiredVisits = 10;
            return View(student);
        }

        public async Task<IActionResult> DownloadCertificate()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var now = DateTime.UtcNow;
            var visits = await streakService.GetVisitCountForMonthAsync(student!.Id, now);
            if (!AttendanceStreakService.IsEligibleForCertificate(visits))
            {
                TempData["Error"] = "You haven't met this month's attendance target yet.";
                return RedirectToAction(nameof(MyGoal));
            }

            var month = now.ToString("MMMM yyyy");
            var svg = BuildCertificateSvg(student.EmpName, visits, month, now);
            var fileName = $"DUT-Gym-Certificate-{student.StudentNumber}-{now:yyyy-MM}.svg";
            return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml", fileName);
        }

        private static string BuildCertificateSvg(string studentName, int visits, string month, DateTime issuedAt)
        {
            var safeName = SecurityElement.Escape(studentName);
            var safeMonth = SecurityElement.Escape(month);
            var issued = SecurityElement.Escape(issuedAt.ToString("dd MMMM yyyy"));

            return $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="1400" height="990" viewBox="0 0 1400 990">
                  <rect width="1400" height="990" fill="#f8fafc"/>
                  <rect x="70" y="70" width="1260" height="850" rx="18" fill="#ffffff" stroke="#0f4c81" stroke-width="12"/>
                  <rect x="105" y="105" width="1190" height="780" rx="10" fill="none" stroke="#e0b44c" stroke-width="4"/>
                  <text x="700" y="190" text-anchor="middle" font-family="Georgia, serif" font-size="62" fill="#0f4c81">Certificate of Consistency</text>
                  <text x="700" y="255" text-anchor="middle" font-family="Arial, sans-serif" font-size="28" fill="#475569">DUT Gym e-Application</text>
                  <text x="700" y="365" text-anchor="middle" font-family="Arial, sans-serif" font-size="30" fill="#334155">This certifies that</text>
                  <text x="700" y="455" text-anchor="middle" font-family="Georgia, serif" font-size="70" fill="#111827">{safeName}</text>
                  <line x1="320" y1="485" x2="1080" y2="485" stroke="#e0b44c" stroke-width="3"/>
                  <text x="700" y="565" text-anchor="middle" font-family="Arial, sans-serif" font-size="32" fill="#334155">completed {visits} gym visits in {safeMonth}</text>
                  <text x="700" y="625" text-anchor="middle" font-family="Arial, sans-serif" font-size="26" fill="#64748b">Awarded for meeting the monthly attendance target of 10 visits.</text>
                  <circle cx="700" cy="735" r="72" fill="#0f4c81"/>
                  <circle cx="700" cy="735" r="58" fill="#ffffff" stroke="#e0b44c" stroke-width="6"/>
                  <text x="700" y="727" text-anchor="middle" font-family="Arial, sans-serif" font-size="20" fill="#0f4c81">DUT</text>
                  <text x="700" y="758" text-anchor="middle" font-family="Arial, sans-serif" font-size="20" fill="#0f4c81">GYM</text>
                  <text x="280" y="835" text-anchor="middle" font-family="Arial, sans-serif" font-size="22" fill="#475569">Issued {issued}</text>
                  <text x="1120" y="835" text-anchor="middle" font-family="Arial, sans-serif" font-size="22" fill="#475569">Verified by attendance records</text>
                </svg>
                """;
        }
    }
}
