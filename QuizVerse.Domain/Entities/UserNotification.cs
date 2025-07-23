using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class UserNotification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int GlobalNotificationId { get; set; }

    public bool IsRead { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Notification GlobalNotification { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
