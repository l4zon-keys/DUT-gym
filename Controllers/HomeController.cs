using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using LoginFormASPCore6.Services;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyDbContext context;
        private readonly GymCapacityService capacityService;
        private readonly PasswordHasher<User> passwordHasher = new();

        public HomeController(MyDbContext context, GymCapacityService capacityService)
        {
            this.context = context;
            this.capacityService = capacityService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserSession") != null)
            {
                return RedirectToRoleDashboard();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(User u)
        {
            var myUser = context.Users.FirstOrDefault(x => x.Email == u.Email);
            var verified = myUser != null
                && passwordHasher.VerifyHashedPassword(myUser, myUser.Password, u.Password) != PasswordVerificationResult.Failed;

            if (verified)
            {
                HttpContext.Session.SetString("UserSession", myUser!.EmpName);
                HttpContext.Session.SetString("UserEmail", myUser.Email);
                HttpContext.Session.SetString("UserRole", myUser.Role);

                return RedirectToRoleDashboard(myUser.Role);
            }
            else
            {
                ViewBag.Message = "Incorrect email or password. Please try again.";
            }
            return View();
        }

        public IActionResult Signup()
        {
            List<SelectListItem> Gender = new()
            {
                new SelectListItem {Value="Male",Text="Male"},
                new SelectListItem {Value="Female",Text="Female"}
            };
            ViewBag.Gender = Gender;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(User u)
        {
            // Role is always derived from the email domain, never trusted from the form.
            u.Role = EmailRoleHelper.GetRole(u.Email);

            if (u.Role == EmailRoleHelper.UnknownRole)
            {
                ModelState.AddModelError(nameof(u.Email),
                    "Only DUT student (@dut4life.ac.za) or staff (@dut.ac.za) email addresses may be used.");
            }

            if (context.Users.Any(x => x.Email == u.Email))
            {
                ModelState.AddModelError(nameof(u.Email), "An account with this email already exists.");
            }

            if (context.Users.Any(x => x.StudentNumber == u.StudentNumber))
            {
                ModelState.AddModelError(nameof(u.StudentNumber), "An account with this student/staff number already exists.");
            }

            if (ModelState.IsValid)
            {
                u.Password = passwordHasher.HashPassword(u, u.Password);
                await context.Users.AddAsync(u);
                await context.SaveChangesAsync();
                TempData["Success"] = "Account created successfully. Please sign in.";
                return RedirectToAction("Login");
            }

            u.Password = string.Empty; // never repopulate the password field on redisplay

            List<SelectListItem> Gender = new()
            {
                new SelectListItem {Value="Male",Text="Male"},
                new SelectListItem {Value="Female",Text="Female"}
            };
            ViewBag.Gender = Gender;
            return View(u);
        }

        public IActionResult BecomeTrainer()
        {
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeTrainer(User u)
        {
            // Unlike normal Signup, role is NOT derived from the email domain here -
            // applying via this form always means "I want to be a Trainer", pending
            // Admin approval.
            u.Role = EmailRoleHelper.TrainerRole;
            u.TrainerApprovalStatus = Models.TrainerApprovalStatus.Pending;

            if (context.Users.Any(x => x.Email == u.Email))
            {
                ModelState.AddModelError(nameof(u.Email), "An account with this email already exists.");
            }

            if (context.Users.Any(x => x.StudentNumber == u.StudentNumber))
            {
                ModelState.AddModelError(nameof(u.StudentNumber), "An account with this ID number already exists.");
            }

            if (ModelState.IsValid)
            {
                u.Password = passwordHasher.HashPassword(u, u.Password);
                await context.Users.AddAsync(u);
                await context.SaveChangesAsync();
                TempData["Success"] = "Application submitted. An admin will review it before you get trainer access.";
                return RedirectToAction("Login");
            }

            u.Password = string.Empty;
            return View(u);
        }

        public async Task<IActionResult> TrainerDashboard()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            if (user.Role != EmailRoleHelper.TrainerRole)
            {
                return RedirectToRoleDashboard(user.Role);
            }

            if (user.TrainerApprovalStatus != Models.TrainerApprovalStatus.Approved)
            {
                return View("TrainerPending", user);
            }

            ViewBag.PendingRequestCount = await context.TrainerRequests
                .CountAsync(r => r.TrainerUserId == user.Id && r.Status == TrainerRequestStatus.Pending);
            ViewBag.AssignedStudentCount = await context.TrainerRequests
                .CountAsync(r => r.TrainerUserId == user.Id && r.Status == TrainerRequestStatus.Accepted);

            return View(user);
        }

        // Legacy / generic entry point: sends an already-logged-in user
        // to the dashboard that matches their role.
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("UserSession") == null)
            {
                return RedirectToAction("Login");
            }
            return RedirectToRoleDashboard();
        }

        public async Task<IActionResult> StudentDashboard()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            if (user.Role != EmailRoleHelper.StudentRole)
            {
                return RedirectToRoleDashboard(user.Role);
            }

            ViewBag.Capacity = await capacityService.GetCurrentStatusAsync();
            ViewBag.Membership = await context.Memberships
                .Include(m => m.Plan)
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.AppliedAt)
                .FirstOrDefaultAsync();

            return View(user);
        }

        public IActionResult StaffDashboard()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            if (user.Role != EmailRoleHelper.StaffRole)
            {
                return RedirectToRoleDashboard(user.Role);
            }
            return View(user);
        }

        public IActionResult AdminDashboard()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            if (user.Role != EmailRoleHelper.AdminRole)
            {
                return RedirectToRoleDashboard(user.Role);
            }
            return View(user);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Logout()
        {
            if (HttpContext.Session.GetString("UserSession") != null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            return RedirectToAction("Login");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // --- Helpers ---------------------------------------------------

        private User? GetCurrentUser()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }
            return context.Users.FirstOrDefault(x => x.Email == email);
        }

        private IActionResult RedirectToRoleDashboard(string? role = null)
        {
            role ??= HttpContext.Session.GetString("UserRole");
            if (role == EmailRoleHelper.AdminRole) return RedirectToAction("AdminDashboard");
            if (role == EmailRoleHelper.StaffRole) return RedirectToAction("StaffDashboard");
            if (role == EmailRoleHelper.TrainerRole) return RedirectToAction("TrainerDashboard");
            return RedirectToAction("StudentDashboard");
        }
    }
}