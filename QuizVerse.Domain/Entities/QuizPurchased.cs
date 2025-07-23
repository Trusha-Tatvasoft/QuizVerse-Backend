using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuizPurchased
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public int UserId { get; set; }

    public DateTime PurchasedDate { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
