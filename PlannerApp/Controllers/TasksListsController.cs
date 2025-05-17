using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlannerApp.Data;
using PlannerApp.Models;
using PlannerApp.Services;
using System.Linq;


namespace PlannerApp.Controllers
{
    [Authorize]
    public class TasksListsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public TasksListsController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public IActionResult Create()
        {
            if (!_permissionService.HasPermission("Создание списка задач"))
                return Forbid();
            return View();
        }

        [HttpPost]
        public IActionResult Create(string title)
        {
            if (!_permissionService.HasPermission("Создание списка задач"))
                return Forbid();

            var list = new TasksList
            {
                UserId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0"),
                Title = title
            };
            _context.TasksLists.Add(list);
            _context.SaveChanges();
            return RedirectToAction("Index", "TasksLists");

        }

        public IActionResult Edit(int id)
        {
            if (!_permissionService.HasPermission("Редактирование списка задач"))
                return Forbid();

            var list = _context.TasksLists.Find(id);
            if (list == null || list.UserId != int.Parse(HttpContext.Session.GetString("UserID") ?? "0"))
                return NotFound();
            return View(list);
        }

        [HttpPost]
        public IActionResult Edit(int id, string title)
        {
            if (!_permissionService.HasPermission("Редактирование списка задач"))
                return Forbid();

            var list = _context.TasksLists.Find(id);
            if (list == null || list.UserId != int.Parse(HttpContext.Session.GetString("UserID") ?? "0"))
                return NotFound();
            list.Title = title;
            _context.SaveChanges();
            return RedirectToAction("Index", "TasksLists");
        }

        public IActionResult Delete(int id)
        {
            if (!_permissionService.HasPermission("Удаление списка задач"))
                return Forbid();

            var userId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0");
            var list = _context.TasksLists
                .Include(l => l.Tasks)
                .FirstOrDefault(l => l.TaskListId == id && l.UserId == userId);
            if (list == null)
                return NotFound();
            if (list.Tasks.Any())
            {
                _context.Tasks.RemoveRange(list.Tasks);
            }
            _context.TasksLists.Remove(list);
            _context.SaveChanges();

            return RedirectToAction("Index", "TasksLists");
        }

        public IActionResult Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserID");

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }
            if (!_permissionService.HasPermission("Создание списка задач"))
                return Forbid();

            var taskLists = _context.TasksLists
                .Where(tl => tl.UserId == userId)
                .OrderBy(tl => tl.Title)
                .ToList();
            var roleId = _context.Users.FirstOrDefault(u => u.UserId == userId)?.RoleId ?? 0;
            ViewBag.Permissions = _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.Title)
                .ToList();

            return View(taskLists);
        }



        



    }
}