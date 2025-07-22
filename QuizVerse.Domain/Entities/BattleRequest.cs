using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleRequest
{
    public int id { get; set; }

    public int sender_id { get; set; }

    public int receiver_id { get; set; }

    public int status { get; set; }

    public DateTime sending_date { get; set; }

    public DateTime? modified_date { get; set; }

    public bool is_deleted { get; set; }

    public virtual User receiver { get; set; } = null!;

    public virtual User sender { get; set; } = null!;
}
