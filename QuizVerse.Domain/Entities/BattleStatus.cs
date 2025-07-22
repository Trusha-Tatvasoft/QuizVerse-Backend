using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleStatus
{
    public int id { get; set; }

    public int battle_id { get; set; }

    public int user1_id { get; set; }

    public int user2_id { get; set; }

    public int battle_status { get; set; }

    public DateTime created_date { get; set; }

    public DateTime? modified_date { get; set; }

    public virtual ICollection<BattleResult> BattleResults { get; set; } = new List<BattleResult>();

    public virtual BattleList battle { get; set; } = null!;

    public virtual User user1 { get; set; } = null!;

    public virtual User user2 { get; set; } = null!;
}
