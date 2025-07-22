using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class Notification
{
    public int id { get; set; }

    public int notification_type { get; set; }

    public string? notification_message { get; set; }

    public int notification_category { get; set; }

    public bool is_global { get; set; }

    public bool is_urgent { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }
}
