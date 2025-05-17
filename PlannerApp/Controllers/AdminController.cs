using Microsoft.AspNetCore.Mvc;
using PlannerApp.Data;
using PlannerApp.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using PlannerApp.Services;
using Microsoft.AspNetCore.Authorization;


namespace PlannerApp.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public AdminController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public IActionResult Index()
        {
            if (!_permissionService.HasPermission("Изменение информации о пользователе"))
                return Forbid();

            var users = _context.Users
                .Include(u => u.Role)
                .ToList();
            return View(users);
        }

        public IActionResult Create()
        {
            if (!_permissionService.HasPermission("Добавление пользователя"))
                return Forbid();

            ViewBag.Roles = _context.Roles.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create(string login, string password, int roleId)
        {
            if (!_permissionService.HasPermission("Добавление пользователя"))
                return Forbid();

            if (_context.Users.Any(u => u.Login == login))
            {
                ViewBag.Error = "Логин занят";
                ViewBag.Roles = _context.Roles.ToList();
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

            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            if (!_permissionService.HasPermission("Изменение информации о пользователе"))
                return Forbid();

            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();
            ViewBag.Roles = _context.Roles.ToList();
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(int id, string login, string password, int roleId)
        {
            if (!_permissionService.HasPermission("Изменение информации о пользователе"))
                return Forbid();

            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();
            if (_context.Users.Any(u => u.Login == login && u.UserId != id))
            {
                ViewBag.Error = "Логин занят";
                ViewBag.Roles = _context.Roles.ToList();
                return View(user);
            }
            user.Login = login;
            if (!string.IsNullOrEmpty(password))
                user.Password = BCrypt.Net.BCrypt.HashPassword(password);
            user.RoleId = roleId;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            if (!_permissionService.HasPermission("Удаление пользователя"))
                return Forbid();

            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
                return NotFound();
            var tasks = _context.Tasks.Where(t => t.UserId == id).ToList();
            _context.Tasks.RemoveRange(tasks);
            var lists = _context.TasksLists.
                Where(l => l.UserId == id).ToList();

            _context.TasksLists.RemoveRange(lists);
            _context.Users.Remove(user);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }


    }
}