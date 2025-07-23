using System;
using System.Collections.Generic;

namespace QuizVerse.Domain.Entities;

public partial class BattleList
{
    public int Id { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int QuizId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public virtual ICollection<BattleQuesDifficultyMap> BattleQuesDifficultyMaps { get; set; } = new List<BattleQuesDifficultyMap>();

    public virtual ICollection<BattleStatus> BattleStatuses { get; set; } = new List<BattleStatus>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ModifiedByNavigation { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;
}
