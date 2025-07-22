using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserNotification
{
    public int id { get; set; }

    public int user_id { get; set; }

    public int global_notification_id { get; set; }

    public bool is_read { get; set; }

    public bool is_deleted { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual Notification global_notification { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }

    public virtual User user { get; set; } = null!;
}
