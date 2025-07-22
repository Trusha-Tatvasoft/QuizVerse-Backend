using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class QuestionDifficulty
{
    public int id { get; set; }

    public string name { get; set; } = null!;

    public string description { get; set; } = null!;

    public int xp_gained { get; set; }

    public bool is_deleted { get; set; }

    public DateTime created_date { get; set; }

    public int created_by { get; set; }

    public DateTime? modified_date { get; set; }

    public int? modified_by { get; set; }

    public virtual ICollection<BaseQuestion> BaseQuestions { get; set; } = new List<BaseQuestion>();

    public virtual ICollection<BattleQuesDifficultyMap> BattleQuesDifficultyMaps { get; set; } = new List<BattleQuesDifficultyMap>();

    public virtual User created_byNavigation { get; set; } = null!;

    public virtual User? modified_byNavigation { get; set; }
}
