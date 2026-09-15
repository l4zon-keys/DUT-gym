using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            return View(student);
        }
    }
}
