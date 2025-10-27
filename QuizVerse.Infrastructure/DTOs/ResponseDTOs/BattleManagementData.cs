using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class BattleManagementData
{
    public int Id { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int BattleTime { get; set; }
    public string BattleName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string BattleDifficulty { get; set; } = null!;
    public int TotalXp { get; set; }
    public int TotalParticipants { get; set; }
    public int TotalQuestion { get; set; }
    public int BattleStatus { get; set; }
}

public class BattleManagementDataResponseDto
{
    public List<BattleManagementData> Battles { get; set; } = new();
    public bool HasMore { get; set; }
}

public class BattleManagementRawResult
{
    public string Battles { get; set; } = string.Empty;

    public bool HasMore { get; set; }
}
