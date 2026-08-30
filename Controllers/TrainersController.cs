using LoginFormASPCore6.Models;
using LoginFormASPCore6.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Personal trainer system (PB-9/10/11): student-facing browse/request/schedule/plans,
    // and trainer-facing request handling/scheduling/plan upload.
    public class TrainersController : AppControllerBase
    {
        private static readonly string[] AllowedPlanExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        private const long MaxPlanFileBytes = 10 * 1024 * 1024;

        private readonly IEmailSender emailSender;
        private readonly IWebHostEnvironment environment;

        public TrainersController(MyDbContext db, IEmailSender emailSender, IWebHostEnvironment environment) : base(db)
        {
            this.emailSender = emailSender;
            this.environment = environment;
        }

        // --- Student side ---------------------------------------------------

        public async Task<IActionResult> Browse()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var trainers = await Db.Users
                .Where(u => u.Role == EmailRoleHelper.TrainerRole && u.TrainerApprovalStatus == TrainerApprovalStatus.Approved)
                .OrderBy(u => u.EmpName)
                .ToListAsync();

            ViewBag.HasActiveRequest = await Db.TrainerRequests.AnyAsync(r => r.StudentUserId == student!.Id
                && (r.Status == TrainerRequestStatus.Pending || r.Status == TrainerRequestStatus.Accepted));

            return View(trainers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestTrainer(int trainerId, string? message)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var trainer = await Db.Users.FirstOrDefaultAsync(u => u.Id == trainerId
                && u.Role == EmailRoleHelper.TrainerRole && u.TrainerApprovalStatus == TrainerApprovalStatus.Approved);
            if (trainer == null) return NotFound();

            var hasActive = await Db.TrainerRequests.AnyAsync(r => r.StudentUserId == student!.Id
                && (r.Status == TrainerRequestStatus.Pending || r.Status == TrainerRequestStatus.Accepted));
            if (hasActive)
            {
                TempData["Error"] = "You already have an active trainer request. Cancel it first if you'd like to request someone else.";
                return RedirectToAction(nameof(Browse));
            }

            Db.TrainerRequests.Add(new TrainerRequest
            {
                StudentUserId = student!.Id,
                TrainerUserId = trainerId,
                Message = message
            });
            await Db.SaveChangesAsync();

            await emailSender.SendAsync(trainer.Email, "New personal trainer request",
                $"{student.EmpName} has requested you as their personal trainer.");

            TempData["Success"] = "Request sent.";
            return RedirectToAction(nameof(MyTrainer));
        }

        public async Task<IActionResult> MyTrainer()
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var request = await Db.TrainerRequests
                .Include(r => r.Trainer)
                .Where(r => r.StudentUserId == student!.Id
                    && (r.Status == TrainerRequestStatus.Pending || r.Status == TrainerRequestStatus.Accepted))
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();

            ViewBag.Sessions = request?.Status == TrainerRequestStatus.Accepted
                ? await Db.TrainerSessions
                    .Where(s => s.StudentUserId == student!.Id && s.TrainerUserId == request.TrainerUserId)
                    .OrderBy(s => s.ScheduledAt)
                    .ToListAsync()
                : new List<TrainerSession>();

            ViewBag.Plans = request?.Status == TrainerRequestStatus.Accepted
                ? await Db.WorkoutPlans
                    .Where(p => p.StudentUserId == student!.Id && p.TrainerUserId == request.TrainerUserId)
                    .OrderByDescending(p => p.UploadedAt)
                    .ToListAsync()
                : new List<WorkoutPlan>();

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var (student, redirect) = RequireStudent();
            if (redirect != null) return redirect;

            var request = await Db.TrainerRequests.FirstOrDefaultAsync(r => r.Id == id && r.StudentUserId == student!.Id);
            if (request == null) return NotFound();
            if (request.Status != TrainerRequestStatus.Pending) return RedirectToAction(nameof(MyTrainer));

            request.Status = TrainerRequestStatus.Cancelled;
            request.RespondedAt = DateTime.UtcNow;
            await Db.SaveChangesAsync();

            return RedirectToAction(nameof(Browse));
        }

        // --- Trainer side ----------------------------------------------------

        public async Task<IActionResult> IncomingRequests()
        {
            var (trainer, redirect) = RequireApprovedTrainer();
            if (redirect != null) return redirect;

            var requests = await Db.TrainerRequests
                .Include(r => r.Student)
                .Where(r => r.TrainerUserId == trainer!.Id && r.Status == TrainerRequestStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToRequest(int id, bool accept)
        {
            var (trainer, redirect) = RequireApprovedTrainer();
            if (redirect != null) return redirect;

            var request = await Db.TrainerRequests.Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.Id == id && r.TrainerUserId == trainer!.Id);
            if (request == null) return NotFound();
            if (request.Status != TrainerRequestStatus.Pending) return RedirectToAction(nameof(IncomingRequests));

            request.Status = accept ? TrainerRequestStatus.Accepted : TrainerRequestStatus.Rejected;
            request.RespondedAt = DateTime.UtcNow;

            if (accept)
            {
                // One trainer at a time: close out any other pending requests this
                // student sent to other trainers.
                var otherPending = await Db.TrainerRequests
                    .Where(r => r.StudentUserId == request.StudentUserId && r.Id != request.Id && r.Status == TrainerRequestStatus.Pending)
                    .ToListAsync();
                foreach (var other in otherPending)
                {
                    other.Status = TrainerRequestStatus.Cancelled;
                    other.RespondedAt = DateTime.UtcNow;
                }
            }

            await Db.SaveChangesAsync();

            await emailSender.SendAsync(request.Student!.Email,
                accept ? "Trainer request accepted" : "Trainer request declined",
                accept
                    ? $"{trainer!.EmpName} accepted your personal trainer request."
                    : $"{trainer!.EmpName} declined your personal trainer request.");

            TempData["Success"] = accept ? "Request accepted." : "Request declined.";
            return RedirectToAction(nameof(IncomingRequests));
        }

        public async Task<IActionResult> MyStudents()
        {
            var (trainer, redirect) = RequireApprovedTrainer();
            if (redirect != null) return redirect;

            var students = await Db.TrainerRequests
                .Include(r => r.Student)
                .Where(r => r.TrainerUserId == trainer!.Id && r.Status == TrainerRequestStatus.Accepted)
                .OrderBy(r => r.Student!.EmpName)
                .ToListAsync();

            return View(students);
        }

        public async Task<IActionResult> StudentDetail(int studentId)
        {
            var (trainer, redirect) = RequireApprovedTrainer();
            if (redirect != null) return redirect;

            var assignment = await Db.TrainerRequests.Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.TrainerUserId == trainer!.Id && r.StudentUserId == studentId && r.Status == TrainerRequestStatus.Accepted);
            if (assignment == null) return NotFound();

            ViewBag.Sessions = await Db.TrainerSessions
                .Where(s => s.TrainerUserId == trainer!.Id && s.StudentUserId == studentId)
                .OrderBy(s => s.ScheduledAt)
                .ToListAsync();

            ViewBag.Plans = await Db.WorkoutPlans
                .Where(p => p.TrainerUserId == trainer!.Id && p.StudentUserId == studentId)
                .OrderByDescending(p => p.UploadedAt)
                .ToListAsync();

            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleSession(int studentId, DateTime scheduledAt, string? notes)
        {
            var (trainer, redirect) = RequireApprovedTrainer();
            if (redirect != null) return redirect;

            var assignment = await Db.TrainerRequests.Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.TrainerUserId == trainer!.Id && r.StudentUserId == studentId && r.Status == TrainerRequestStatus.Accepted);
            if (assignment == null) return NotFound();

            var session = new TrainerSession
            {
                TrainerUserId = trainer!.Id,
                StudentUserId = studentId,
                ScheduledAt = scheduledAt,
                Notes = notes
            };
            Db.TrainerSessions.Add(session);
            await Db.SaveChangesAsync();

            var when = scheduledAt.ToString("f");
            await emailSender.SendAsync(assignment.Student!.Email, "Personal trainer session scheduled",
                $"{trainer.EmpName} scheduled a session with you on {when}.");
            await emailSender.SendAsync(trainer.Email, "Personal trainer session scheduled",
                $"You scheduled a session with {assignment.Student.EmpName} on {when}.");

            TempData["Success"] = "Session scheduled.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPlan(int studentId, string title, string? notes, IFormFile planFile)
        {
            var (trainer, redirect) = RequireApprovedTrainer();
            if (redirect != null) return redirect;

            var assignment = await Db.TrainerRequests
                .FirstOrDefaultAsync(r => r.TrainerUserId == trainer!.Id && r.StudentUserId == studentId && r.Status == TrainerRequestStatus.Accepted);
            if (assignment == null) return NotFound();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Please give the plan a title.";
                return RedirectToAction(nameof(StudentDetail), new { studentId });
            }

            if (planFile == null || planFile.Length == 0)
            {
                TempData["Error"] = "Please choose a file to upload.";
                return RedirectToAction(nameof(StudentDetail), new { studentId });
            }

            var extension = Path.GetExtension(planFile.FileName).ToLowerInvariant();
            if (!AllowedPlanExtensions.Contains(extension))
            {
                TempData["Error"] = "Only PDF, JPG, PNG, DOC, or DOCX files are accepted.";
                return RedirectToAction(nameof(StudentDetail), new { studentId });
            }

            if (planFile.Length > MaxPlanFileBytes)
            {
                TempData["Error"] = "File is too large (10MB max).";
                return RedirectToAction(nameof(StudentDetail), new { studentId });
            }

            var relativeDir = Path.Combine("uploads", "workoutplans", studentId.ToString());
            var absoluteDir = Path.Combine(environment.WebRootPath, relativeDir);
            Directory.CreateDirectory(absoluteDir);

            var generatedFileName = $"{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(absoluteDir, generatedFileName);
            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await planFile.CopyToAsync(stream);
            }

            Db.WorkoutPlans.Add(new WorkoutPlan
            {
                TrainerUserId = trainer!.Id,
                StudentUserId = studentId,
                Title = title,
                Notes = notes,
                FilePath = Path.Combine(relativeDir, generatedFileName).Replace('\\', '/')
            });
            await Db.SaveChangesAsync();

            TempData["Success"] = "Workout plan uploaded.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        public async Task<IActionResult> MySchedule()
        {
            var (trainer, redirect) = RequireApprovedTrainer();
            if (redirect != null) return redirect;

            var sessions = await Db.TrainerSessions
                .Include(s => s.Student)
                .Where(s => s.TrainerUserId == trainer!.Id)
                .OrderBy(s => s.ScheduledAt)
                .ToListAsync();

            return View(sessions);
        }
    }
}
