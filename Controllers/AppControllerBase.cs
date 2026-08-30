using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace LoginFormASPCore6.Controllers
{
    // Shared session/role helpers for controllers beyond HomeController.
    public abstract class AppControllerBase : Controller
    {
        protected readonly MyDbContext Db;

        protected AppControllerBase(MyDbContext db)
        {
            Db = db;
        }

        protected User? GetCurrentUser()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }
            return Db.Users.FirstOrDefault(x => x.Email == email);
        }

        protected (User? User, IActionResult? Redirect) RequireRole(params string[] roles)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return (null, RedirectToAction("Login", "Home"));
            }
            if (!roles.Contains(user.Role))
            {
                return (null, RedirectToAction("Dashboard", "Home"));
            }
            return (user, null);
        }

        // Admin has every Staff permission plus admin-only ones, so anything gated to
        // "staff" also lets an Admin through. A Staff account still needs Admin
        // approval first, though - Admin itself is manually promoted and never
        // gated by ApprovalStatus.
        protected (User? User, IActionResult? Redirect) RequireStaff()
        {
            var (user, redirect) = RequireRole(EmailRoleHelper.StaffRole, EmailRoleHelper.AdminRole);
            if (redirect != null) return (null, redirect);
            if (user!.Role == EmailRoleHelper.StaffRole && user.ApprovalStatus != ApprovalStatus.Approved)
            {
                return (null, RedirectToAction("Dashboard", "Home"));
            }
            return (user, null);
        }

        protected (User? User, IActionResult? Redirect) RequireAdmin() =>
            RequireRole(EmailRoleHelper.AdminRole);

        protected (User? User, IActionResult? Redirect) RequireStudent() => RequireRole(EmailRoleHelper.StudentRole);

        // Any logged-in trainer, approved or not - callers that need to show a
        // "pending approval" holding page use this and check ApprovalStatus
        // themselves. Use RequireApprovedTrainer() for actual trainer features.
        protected (User? User, IActionResult? Redirect) RequireTrainer() => RequireRole(EmailRoleHelper.TrainerRole);

        protected (User? User, IActionResult? Redirect) RequireApprovedTrainer()
        {
            var (user, redirect) = RequireTrainer();
            if (redirect != null) return (null, redirect);
            if (user!.ApprovalStatus != ApprovalStatus.Approved)
            {
                return (null, RedirectToAction("TrainerDashboard", "Home"));
            }
            return (user, null);
        }

        protected (User? User, IActionResult? Redirect) RequireAnyUser()
        {
            var user = GetCurrentUser();
            return user == null ? (null, RedirectToAction("Login", "Home")) : (user, null);
        }
    }
}
