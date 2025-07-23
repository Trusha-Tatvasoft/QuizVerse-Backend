using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class PaymentPlatform
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<GlobalPayment> GlobalPayments { get; set; } = new List<GlobalPayment>();

    public virtual User? ModifiedByNavigation { get; set; }
}
