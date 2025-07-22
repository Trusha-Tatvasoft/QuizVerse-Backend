using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleResult
{
    public int id { get; set; }

    public int battle_status { get; set; }

    public int? winner_id { get; set; }

    public int winner_gained_xp { get; set; }

    public int looser_gained_xp { get; set; }

    public int user1_corrected_ans { get; set; }

    public int user2_corrected_ans { get; set; }

    public DateTime user1_taken_time { get; set; }

    public DateTime user2_taken_time { get; set; }

    public virtual BattleStatus battle_statusNavigation { get; set; } = null!;

    public virtual User? winner { get; set; }
}
