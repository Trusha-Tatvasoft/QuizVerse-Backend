using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class GlobalPayment
{
    public int Id { get; set; }

    public int PaidByUserId { get; set; }

    public string ReceivedBy { get; set; } = null!;

    public int PaymentAmount { get; set; }

    public string PaymentId { get; set; } = null!;

    public int PaymentStatus { get; set; }

    public int PaymentPlatformId { get; set; }

    public int PaymentTypeId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual User PaidByUser { get; set; } = null!;

    public virtual PaymentPlatform PaymentPlatform { get; set; } = null!;

    public virtual PaymentType PaymentType { get; set; } = null!;
}
