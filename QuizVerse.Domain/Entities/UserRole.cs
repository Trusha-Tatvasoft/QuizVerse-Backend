using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserRole
{
    public int id { get; set; }

    public string name { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
