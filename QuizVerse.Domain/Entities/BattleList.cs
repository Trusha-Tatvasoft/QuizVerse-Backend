using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleList
{
    public int id { get; set; }

    public DateTime start_date { get; set; }

    public DateTime end_date { get; set; }

    public int quiz_id { get; set; }

    public bool is_deleted { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<BattleQuesDifficultyMap> BattleQuesDifficultyMaps { get; set; } = new List<BattleQuesDifficultyMap>();

    public virtual ICollection<BattleStatus> BattleStatuses { get; set; } = new List<BattleStatus>();

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }

    public virtual Quiz quiz { get; set; } = null!;
}
