using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // XP/levels/badges/rewards (achievements, display-only), challenges, and
    // XP leaderboards. Kept separate from GoalsController.Leaderboard, which is
    // the pre-existing attendance-visits leaderboard.
    public class GamificationController : AppControllerBase
    {
        private readonly GamificationService gamificationService;
        private readonly FriendshipService friendshipService;

        public GamificationController(MyDbContext db, GamificationService gamificationService, FriendshipService friendshipService) : base(db)
        {
            this.gamificationService = gamificationService;
            this.friendshipService = friendshipService;
        }

        // --- Achievements profile ------------------------------------------

        public async Task<IActionResult> Profile()
        {
            var (user, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            ViewBag.XpSummary = await gamificationService.GetXpLevelSummaryAsync(user!.Id);
            ViewBag.Badges = await gamificationService.GetEarnedBadgesAsync(user.Id);
            ViewBag.Rewards = await gamificationService.GetEarnedRewardsAsync(user.Id);

            return View(user);
        }

        // --- Challenges -------------------------------------------------------

        public async Task<IActionResult> Challenges()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var now = DateTime.UtcNow;
            var challenges = await Db.Challenges
                .Where(c => c.EndDate >= now)
                .OrderBy(c => c.EndDate)
                .ToListAsync();

            var myParticipation = await Db.ChallengeParticipants
                .Where(p => p.UserId == student!.Id)
                .ToDictionaryAsync(p => p.ChallengeId);

            ViewBag.MyParticipation = myParticipation;
            return View(challenges);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinChallenge(int challengeId)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var challenge = await Db.Challenges.FindAsync(challengeId);
            if (challenge == null) return NotFound();

            var alreadyJoined = await Db.ChallengeParticipants.AnyAsync(p => p.ChallengeId == challengeId && p.UserId == student!.Id);
            if (!alreadyJoined)
            {
                Db.ChallengeParticipants.Add(new ChallengeParticipant { ChallengeId = challengeId, UserId = student!.Id });
                await Db.SaveChangesAsync();
                TempData["Success"] = "Joined challenge.";
            }

            return RedirectToAction(nameof(Challenges));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogChallengeProgress(int challengeId, decimal value)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var participant = await Db.ChallengeParticipants
                .Include(p => p.Challenge)
                .FirstOrDefaultAsync(p => p.ChallengeId == challengeId && p.UserId == student!.Id);
            if (participant == null) return NotFound();

            participant.CurrentValue = value;

            var justCompleted = !participant.Completed && GamificationService.IsChallengeComplete(value, participant.Challenge!.TargetValue);
            if (justCompleted)
            {
                participant.Completed = true;
                participant.CompletedAt = DateTime.UtcNow;
            }
            await Db.SaveChangesAsync();

            if (justCompleted)
            {
                await gamificationService.AwardXpAsync(student!.Id, gamificationService.ChallengeCompletedPoints, XpReason.ChallengeCompleted, participant.Id);
                await gamificationService.EvaluateAndAwardRewardsAsync(student.Id);
                TempData["Success"] = "Progress logged - challenge complete!";
            }
            else
            {
                TempData["Success"] = "Progress logged.";
            }

            return RedirectToAction(nameof(Challenges));
        }

        // --- Leaderboards -----------------------------------------------------

        public async Task<IActionResult> Leaderboard(string period = "weekly")
        {
            var (user, redirect) = RequireAnyUser();
            if (redirect != null) return redirect;

            var now = DateTime.UtcNow;
            List<GamificationLeaderboardEntry> entries;

            if (period == "friends")
            {
                entries = await gamificationService.GetFriendsLeaderboardAsync(user!.Id);
            }
            else if (period == "monthly")
            {
                var start = new DateTime(now.Year, now.Month, 1);
                entries = await gamificationService.GetXpLeaderboardAsync(start, start.AddMonths(1));
            }
            else
            {
                period = "weekly";
                var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                entries = await gamificationService.GetXpLeaderboardAsync(weekStart, weekStart.AddDays(7));
            }

            ViewBag.Period = period;
            return View(entries);
        }

        // --- Friends ------------------------------------------------------------

        public async Task<IActionResult> Friends()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            ViewBag.Friends = await friendshipService.GetAcceptedFriendsAsync(student!.Id);
            ViewBag.PendingRequests = await friendshipService.GetPendingIncomingRequestsAsync(student.Id);
            ViewBag.CurrentUserId = student.Id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendFriendRequest(string identifier)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var term = identifier?.Trim() ?? string.Empty;
            var target = await Db.Users.FirstOrDefaultAsync(u => u.Email == term || u.StudentNumber == term);
            if (target == null)
            {
                TempData["Error"] = "No student found with that email or student number.";
                return RedirectToAction(nameof(Friends));
            }

            var (ok, error) = await friendshipService.SendRequestAsync(student!.Id, target.Id);
            TempData[ok ? "Success" : "Error"] = ok ? "Friend request sent." : error;
            return RedirectToAction(nameof(Friends));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriendRequest(int requestId)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            await friendshipService.AcceptRequestAsync(student!.Id, requestId);
            TempData["Success"] = "Friend request accepted.";
            return RedirectToAction(nameof(Friends));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriend(int friendshipId)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            await friendshipService.RemoveAsync(student!.Id, friendshipId);
            return RedirectToAction(nameof(Friends));
        }

        // --- Admin: manage challenges --------------------------------------

        public async Task<IActionResult> ManageChallenges()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var challenges = await Db.Challenges.OrderByDescending(c => c.StartDate).ToListAsync();
            return View(challenges);
        }

        public IActionResult CreateChallenge()
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;
            return View(new Challenge());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChallenge(Challenge challenge)
        {
            var (admin, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            ModelState.Remove(nameof(Models.Challenge.CreatedByUserId));
            if (challenge.EndDate <= challenge.StartDate)
            {
                ModelState.AddModelError(nameof(Models.Challenge.EndDate), "End date must be after the start date.");
            }
            if (!ModelState.IsValid)
            {
                return View(challenge);
            }

            challenge.CreatedByUserId = admin!.Id;
            Db.Challenges.Add(challenge);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Challenge created.";
            return RedirectToAction(nameof(ManageChallenges));
        }

        public async Task<IActionResult> EditChallenge(int id)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var challenge = await Db.Challenges.FindAsync(id);
            if (challenge == null) return NotFound();
            return View(challenge);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChallenge(int id, Challenge challenge)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;
            if (id != challenge.Id) return NotFound();

            ModelState.Remove(nameof(Models.Challenge.CreatedByUserId));
            if (challenge.EndDate <= challenge.StartDate)
            {
                ModelState.AddModelError(nameof(Models.Challenge.EndDate), "End date must be after the start date.");
            }
            if (!ModelState.IsValid)
            {
                return View(challenge);
            }

            var existing = await Db.Challenges.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Title = challenge.Title;
            existing.Description = challenge.Description;
            existing.Metric = challenge.Metric;
            existing.TargetValue = challenge.TargetValue;
            existing.StartDate = challenge.StartDate;
            existing.EndDate = challenge.EndDate;
            await Db.SaveChangesAsync();

            TempData["Success"] = "Challenge updated.";
            return RedirectToAction(nameof(ManageChallenges));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChallenge(int id)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var challenge = await Db.Challenges.FindAsync(id);
            if (challenge == null) return NotFound();

            Db.Challenges.Remove(challenge);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Challenge deleted.";
            return RedirectToAction(nameof(ManageChallenges));
        }
    }
}
