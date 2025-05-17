using System;
using System.Collections.Generic;

namespace PlannerApp.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
