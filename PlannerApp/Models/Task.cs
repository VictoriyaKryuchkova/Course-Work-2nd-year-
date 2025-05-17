using System;
using System.Collections.Generic;

namespace PlannerApp.Models;

public partial class Task
{
    public int TaskId { get; set; }

    public int? UserId { get; set; }

    public int? TaskListId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime DateCreate { get; set; }

    public DateTime DateEnd { get; set; }

    public int? CategoryId { get; set; }

    public int? StatusId { get; set; }

    public bool Reminder { get; set; }

    public string? Description { get; set; }

    public DateTime? ReminderTime { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Status? Status { get; set; }

    public virtual TasksList? TaskList { get; set; }

    public virtual User? User { get; set; }
}
