using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class GlobalPayment
{
    public int id { get; set; }

    public int paid_by_user_id { get; set; }

    public string received_by { get; set; } = null!;

    public int payment_amount { get; set; }

    public string payment_id { get; set; } = null!;

    public int payment_status { get; set; }

    public int payment_platform_id { get; set; }

    public int payment_type_id { get; set; }

    public string? description { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }

    public virtual User paid_by_user { get; set; } = null!;

    public virtual PaymentPlatform payment_platform { get; set; } = null!;

    public virtual PaymentType payment_type { get; set; } = null!;
}
