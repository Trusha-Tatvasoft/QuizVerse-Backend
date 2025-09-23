using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class UserBattleHistoryRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "BatchNumber must be greater than 0")]
    public int BatchNumber { get; set; } = 1;

    public BattleFilterType? FilterBy { get; set; }
    public BattleTimeFilterType? TimeFilterBy { get; set; }
}


public class UserBattleHistoryRawResult
{
    [Column("battles")]
    public string Battles { get; set; } = string.Empty;

    [Column("hasmore")]
    public bool HasMore { get; set; }
}
