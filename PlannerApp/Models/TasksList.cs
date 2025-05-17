using System;
using System.Collections.Generic;

namespace PlannerApp.Models;

public partial class TasksList
{
    public int TaskListId { get; set; }

    public int? UserId { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual User? User { get; set; }
}
