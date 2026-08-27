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

        protected (User? User, IActionResult? Redirect) RequireRole(string role)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return (null, RedirectToAction("Login", "Home"));
            }
            if (user.Role != role)
            {
                return (null, RedirectToAction("Dashboard", "Home"));
            }
            return (user, null);
        }

        protected (User? User, IActionResult? Redirect) RequireStaff() => RequireRole(EmailRoleHelper.StaffRole);

        protected (User? User, IActionResult? Redirect) RequireStudent() => RequireRole(EmailRoleHelper.StudentRole);

        protected (User? User, IActionResult? Redirect) RequireAnyUser()
        {
            var user = GetCurrentUser();
            return user == null ? (null, RedirectToAction("Login", "Home")) : (user, null);
        }
    }
}
