using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleResult
{
    public int Id { get; set; }

    public int BattleStatus { get; set; }

    public int? WinnerId { get; set; }

    public int WinnerGainedXp { get; set; }

    public int LooserGainedXp { get; set; }

    public int User1CorrectedAns { get; set; }

    public int User2CorrectedAns { get; set; }

    public DateTime User1TakenTime { get; set; }

    public DateTime User2TakenTime { get; set; }

    public virtual BattleStatus BattleStatusNavigation { get; set; } = null!;

    public virtual User? Winner { get; set; }
}
