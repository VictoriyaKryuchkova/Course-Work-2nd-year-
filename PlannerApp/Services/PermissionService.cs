using PlannerApp.Data;

namespace PlannerApp.Services
{
    public class PermissionService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContext;

        public PermissionService(AppDbContext context, IHttpContextAccessor httpContext)
        {
            _context = context;
            _httpContext = httpContext;
        }

        public bool HasPermission(string permissionTitle)
        {
            var userId = int.Parse(_httpContext.HttpContext.Session.GetString("UserID") ?? "0");
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return false;

            return _context.RolePermissions
                .Any(rp => rp.RoleId == user.RoleId && rp.Permission.Title == permissionTitle);
        }
    }
}