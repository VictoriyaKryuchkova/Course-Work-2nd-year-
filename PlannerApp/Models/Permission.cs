using System;
using System.Collections.Generic;

namespace PlannerApp.Models;

public partial class Permission
{
    public int PermissionId { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
