using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleRequest
{
    public int Id { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public int Status { get; set; }

    public DateTime SendingDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
