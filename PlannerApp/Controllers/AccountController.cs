using Microsoft.AspNetCore.Mvc;
using PlannerApp.Data;
using PlannerApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using PlannerApp.Services;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PlannerApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public AccountController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string login, string password)
        {
            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Login == login);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                ViewBag.Error = "Неверный логин или пароль";
                return View();
            }

            if (user.Role == null)
            {
                ViewBag.Error = "У пользователя отсутствует роль";
                return View();
            }

            HttpContext.Session.SetString("UserID", user.UserId.ToString());
            HttpContext.Session.SetString("Role", user.Role.Title);


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, user.Role.Title)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return user.Role.Title == "admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Tasks");
        }

        

        public IActionResult Register()
        {
            ViewBag.Roles = _context.Roles.Where(r => r.Title == "BaseUser" || r.Title == "BusinessUser").ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Register(string login, string password, int roleId)
        {
            if (_context.Users.Any(u => u.Login == login))
            {
                ViewBag.Error = "Логин уже занят";
                ViewBag.Roles = _context.Roles.Where(r => r.Title == "BaseUser" || r.Title == "BusinessUser").ToList();
                return View();
            }

            var user = new User
            {
                Login = login,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = roleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction("Index", "Home");
        }
    }
}