using Microsoft.AspNetCore.Mvc;
using PlannerApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using PlannerApp.Services;
using ClosedXML.Excel;


namespace PlannerApp.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public TasksController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public IActionResult Index(string sortBy, bool groupByCategory = false)
        {
            if (!_permissionService.HasPermission("Сортировка задач"))
                return Forbid();

            var userId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0");
            var tasks = _context.Tasks
                .Include(t => t.TaskList)
                .Include(t => t.Category)
                .Include(t => t.Status)
                .Where(t => t.UserId == userId);

            switch (sortBy)
            {
                case "Title":
                    tasks = tasks.OrderBy(t => t.Title);
                    break;
                case "DateEnd":
                    tasks = tasks.OrderBy(t => t.DateEnd);
                    break;
                case "Status":
                    tasks = tasks.OrderBy(t => t.Status.Title != "Выполнена");
                    break;
                default:
                    tasks = tasks.OrderBy(t => t.TaskId);
                    break;
            }
            ViewBag.GroupByCategory = groupByCategory;
            ViewBag.CurrentSort = sortBy;
            ViewBag.TaskLists = _context.TasksLists.Where(tl => tl.UserId == userId).ToList();
            ViewBag.Statuses = _context.Statuses.ToList();
            ViewBag.Permissions = _context.RolePermissions
                .Where(rp => rp.RoleId == _context.Users.FirstOrDefault(u => u.UserId == userId).RoleId)
                .Select(rp => rp.Permission.Title)
                .ToList();

            return View(tasks.ToList());
        }

        public IActionResult Create(int? taskListId = null)
        {
            if (!_permissionService.HasPermission("Создание задачи"))
                return Forbid();
            int userId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0");

            ViewBag.TaskLists = _context.TasksLists
                .Where(tl => tl.UserId == userId)
                .ToList();

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Statuses = _context.Statuses.ToList();
            ViewBag.SelectedTaskListId = taskListId;
            var roleId = _context.Users.FirstOrDefault(u => u.UserId == userId)?.RoleId ?? 0;
            var permissions = _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.Title)
                .ToList();

            ViewBag.CanUseTaskLists = permissions.Contains("Создание списка задач");
            return View();
        }


        //создание задачи
        [HttpPost]
        public IActionResult Create(string title, DateTime dateEnd, int? taskListId, int? categoryId, int? statusId, bool reminder, string description, DateTime? reminderTime)
        {
            if (!_permissionService.HasPermission("Создание задачи"))
                return Forbid();
            if (!statusId.HasValue)
            {
                statusId = _context.Statuses.FirstOrDefault(s => s.Title == "Не выполнена")?.StatusId;
            }
            var task = new Models.Task
            {
                UserId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0"),
                TaskListId = taskListId,
                Title = title,
                DateCreate = DateTime.Now,
                DateEnd = dateEnd,
                CategoryId = categoryId,
                StatusId = statusId,
                Reminder = reminder,
                Description = description,
                ReminderTime = reminderTime
            };
            _context.Tasks.Add(task);
            _context.SaveChanges();

            if (taskListId.HasValue)
                return RedirectToAction("ByList", new { id = taskListId });

            return RedirectToAction("Index");

        }

        public IActionResult Edit(int id)
        {
            //проверка возможностей
            if (!_permissionService.HasPermission("Редактирование задачи"))
                return Forbid();

            var task = _context.Tasks.Find(id);
            if (task == null || task.UserId != int.Parse(HttpContext.Session.GetString("UserID") ?? "0"))
                return NotFound();
            ViewBag.TaskLists = _context.TasksLists
                .Where(tl => tl.UserId == task.UserId)
                .ToList();

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Statuses = _context.Statuses.ToList();
            return View(task);
        }

        [HttpPost]
        public IActionResult Edit(int id, string title, DateTime dateEnd, int? taskListId, int? categoryId, int? statusId, bool reminder, string description, DateTime? reminderTime)
        {
            if (!_permissionService.HasPermission("Редактирование задачи"))
                return Forbid();

            var task = _context.Tasks.Find(id);
            if (task == null || task.UserId != int.Parse(HttpContext.Session.GetString("UserID") ?? "0"))
                return NotFound();
            task.Title = title;
            task.DateEnd = dateEnd;
            task.TaskListId = taskListId;
            task.CategoryId = categoryId;
            task.StatusId = statusId;
            task.Reminder = reminder;
            task.Description = description;
            task.ReminderTime = reminderTime;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        public IActionResult Delete(int id)
        {
            if (!_permissionService.HasPermission("Удаление задачи"))
                return Forbid();

            var task = _context.Tasks.Find(id);
            if (task == null || task.UserId != int.Parse(HttpContext.Session.GetString("UserID") ?? "0"))
                return NotFound();
            _context.Tasks.Remove(task);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            // 1 -не выполнена, 2 - выполнена
            task.StatusId = (task.StatusId == 2) ? 1 : 2;
            _context.Update(task);
            await _context.SaveChangesAsync();
            return Ok(); 
        }



        public IActionResult MoveToList(int id, int? listId)
        {
            if (!_permissionService.HasPermission("Перемещение задач между списками"))
                return Forbid();
            var task = _context.Tasks.Find(id);
            if (task == null || task.UserId != int.Parse(HttpContext.Session.GetString("UserID") ?? "0"))
                return NotFound();
            task.TaskListId = listId;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        //календарь с задачами
        public IActionResult Calendar()
        {
            if (!_permissionService.HasPermission("Календарное представление задач"))
                return Forbid();

            var userId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0");
            var tasks = _context.Tasks
                .Include(t => t.Status)
                .Where(t => t.UserId == userId)
                .ToList();
            return View(tasks);
        }

        //группировка задач по спикам
        public IActionResult ByList(int id)
        {
            if (!_permissionService.HasPermission("Создание задачи"))
                return Forbid();

            var userId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0");

            var tasks = _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Status)
                .Include(t => t.TaskList)
                .Where(t => t.UserId == userId && t.TaskListId == id)
                .ToList();

            ViewBag.TaskListId = id;
            ViewBag.TaskListTitle = _context.TasksLists.FirstOrDefault(tl => tl.TaskListId == id)?.Title ?? "Список задач";

            var roleId = _context.Users.FirstOrDefault(u => u.UserId == userId)?.RoleId ?? 0;
            ViewBag.Permissions = _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.Title)
                .ToList();

            ViewBag.TaskLists = _context.TasksLists.Where(tl => tl.UserId == userId).ToList();

            return View("Index",tasks);
        }

       // отчеты в эксель отображние
        public IActionResult Reports(DateTime? startDate, DateTime? endDate)
        {
            if (!_permissionService.HasPermission("Формирование отчета"))
                return Forbid();

            var userId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0");

            var tasks = _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Status)
                .Where(t => t.UserId == userId)
                .Where(t => (!startDate.HasValue || t.DateCreate >= startDate) &&
                            (!endDate.HasValue || t.DateEnd <= endDate))
                .ToList();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(tasks);
        }


        //скачивание эксель отчетов
        [HttpPost]
        public IActionResult ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserID") ?? "0");

            var tasks = _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Status)
                .Where(t => t.UserId == userId)
                .Where(t => (!startDate.HasValue || t.DateCreate >= startDate) &&
                            (!endDate.HasValue || t.DateEnd <= endDate))
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var bum = workbook.Worksheets.Add("Отчет по задачам");
                bum.Cell(1, 1).Value = "Название задачи";
                bum.Cell(1, 2).Value = "Категория";
                bum.Cell(1, 3).Value = "Статус";
                bum.Cell(1, 4).Value = "Дата создания";
                bum.Cell(1, 5).Value = "Срок окончания";

                for (int i = 0; i < tasks.Count; i++)
                {
                    var t = tasks[i];
                    bum.Cell(i + 2, 1).Value = t.Title;
                    bum.Cell(i + 2, 2).Value = t.Category?.Title ?? "-";
                    bum .Cell(i + 2, 3).Value = t.Status?.Title ?? "-";
                    bum.Cell(i + 2, 4).Value = t.DateCreate.ToString("dd.MM.yyyy");
                    bum .Cell(i + 2, 5).Value = t.DateEnd.ToString("dd.MM.yyyy");
                }

                bum.Column(1).Width = 30;
                bum.Column(2).Width = 20;
                bum.Column(3).Width = 20;
                bum.Column(4).Width = 15;
                bum.Column(5).Width = 15;

                using (var p = new MemoryStream())
                {
                    workbook.SaveAs(p);
                    p.Position = 0;
                    var fileName = $"Отчет_по_задачам_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(p.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }


    }
}
