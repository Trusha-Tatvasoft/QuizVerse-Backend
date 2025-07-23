using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleQuesDifficultyMap
{
    public int Id { get; set; }

    public int BattleId { get; set; }

    public int QueDifficultyId { get; set; }

    public int NoOfQues { get; set; }

    public DateTime QusTime { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual BattleList Battle { get; set; } = null!;

    public virtual QuestionDifficulty QueDifficulty { get; set; } = null!;
}
