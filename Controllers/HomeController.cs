using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LoginFormASPCore6.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyDbContext context;

        public HomeController(MyDbContext context)
        {
            this.context = context;
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
        public IActionResult Login(User u)
        {
            var myUser = context.Users
                .Where(x => x.Email == u.Email && x.Password == u.Password)
                .FirstOrDefault();

            if (myUser != null)
            {
                HttpContext.Session.SetString("UserSession", myUser.EmpName);
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

            if (ModelState.IsValid)
            {
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

        public IActionResult StudentDashboard()
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
            return role == EmailRoleHelper.StaffRole
                ? RedirectToAction("StaffDashboard")
                : RedirectToAction("StudentDashboard");
        }
    }
}