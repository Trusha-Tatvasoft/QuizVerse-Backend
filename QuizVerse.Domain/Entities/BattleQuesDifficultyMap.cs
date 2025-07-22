using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleQuesDifficultyMap
{
    public int id { get; set; }

    public int battle_id { get; set; }

    public int que_difficulty_id { get; set; }

    public int no_of_ques { get; set; }

    public DateTime qus_time { get; set; }

    public DateTime created_date { get; set; }

    public virtual BattleList battle { get; set; } = null!;

    public virtual QuestionDifficulty que_difficulty { get; set; } = null!;
}
