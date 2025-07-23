using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class Notification
{
    public int Id { get; set; }

    public int NotificationType { get; set; }

    public string? NotificationMessage { get; set; }

    public int NotificationCategory { get; set; }

    public bool IsGlobal { get; set; }

    public bool IsUrgent { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
}
