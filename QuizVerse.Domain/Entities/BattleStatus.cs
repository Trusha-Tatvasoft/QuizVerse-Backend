using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleStatus
{
    public int Id { get; set; }

    public int BattleId { get; set; }

    public int User1Id { get; set; }

    public int User2Id { get; set; }

    public int BattleStatus1 { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual BattleList Battle { get; set; } = null!;

    public virtual ICollection<BattleResult> BattleResults { get; set; } = new List<BattleResult>();

    public virtual User User1 { get; set; } = null!;

    public virtual User User2 { get; set; } = null!;
}
