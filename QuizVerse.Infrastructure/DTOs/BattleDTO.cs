using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Infrastructure.DTOs;

public class BattleState
{
    public int BattleAttemptId { get; init; }
    public string? Player1ConnectionId { get; set; }
    public int Player1Id { get; set; }
    public string? Player2ConnectionId { get; set; }
    public int Player2Id { get; set; }
    public int TotalQuestions { get; init; }
    public List<QuestionsDetail> AttemptedQuestionsDetails { get; init; } = new();
    public ConcurrentDictionary<int, int> CurrentIndex { get; } = new();
    public ConcurrentDictionary<int, int> Score { get; } = new();
    public ConcurrentDictionary<int, CancellationTokenSource> ActiveTimers { get; } = new();
    public ConcurrentDictionary<int, bool> Completed { get; } = new();
    public ConcurrentDictionary<int, bool> Connected { get; } = new();
    public ConcurrentDictionary<int, DateTime> ConnectionBrokeTime { get; } = new();
    public ConcurrentDictionary<int, bool> IsSkipInstruction { get; set; } = new();
    public bool IsReadyForStart => !string.IsNullOrEmpty(Player1ConnectionId) && !string.IsNullOrEmpty(Player2ConnectionId);
}

public class QuestionsDetail
{
    public int Index { get; set; }
    public int QuestionId { get; set; }
    public int Xp { get; set; }
    public int TimeLimit { get; set; }
    public ConcurrentDictionary<int, int> TimeTaken { get; } = new();
    public ConcurrentDictionary<int, DateTime> QuestionGivenTime { get; } = new();
    public ConcurrentDictionary<int, bool> IsCorrect { get; } = new();
}

public class SubmitAnswerDTO
{
    [Required(ErrorMessage = "Battle status id is required")]
    public int battleStatusID { get; set; }
    [Required(ErrorMessage = "Answer id is required")]
    public int questionId { get; set; }
    [Required(ErrorMessage = "Answer is required")]
    public string answer { get; set; }
}

public class ScoreUpdateDto
{
    public int BattleAttemptId { get; set; }
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
}

[Keyless]
public class RawBattleQuestionDto
{
    [Column("quiz_question_id")]
    public int QuizQuestionId { get; set; }
    [Column("question_name")]
    public string QuestionName { get; set; } = null!;
    [Column("question_type")]
    public string QuestionType { get; set; } = null!;
    [Column("options")]
    public string Options { get; set; } = "[]";
    [Column("time_per_question")]
    public int Time { get; set; }
    [Column("xp_per_question")]
    public int Xp { get; set; }
}

public class BattleQuestionResponseDto
{
    public int QuestionIndex { get; set; }
    public int QuizQuestionId { get; set; }
    public string QuestionName { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public List<OptionResponseDto> Options { get; set; } = new();
    public int TimeInSeconds { get; set; }
}

public record LastAnswerdQuestionDetail(int QuestionIndex, bool IsCorrect, int XpGained, string CorrectAnswer);

public record ScoreChangedDto(int BattleAttemptId, int Player1Score, int Player2Score, int Player1Id, int Player2Id, int Player1CurrentIndex, int Player2CurrentIndex, int Player1CorrectedAns, int Player2CorrectedAns);

public record NextQuestionDto(string ConnectionId, BattleQuestionResponseDto Question);

public record BattleFinishedDto(int BattleId, BattleResult Result);

public class SubmitAnswerResult
{
    public ScoreChangedDto? ScoreChanged { get; init; }
    public NextQuestionDto? NextQuestion { get; init; }
    public BattleFinishedDto? Finished { get; init; }
    public string? ErrorMessage { get; init; }
    public LastAnswerdQuestionDetail? LastAnswerdQuestionDetail { get; init; }
}

public class TimeoutResult
{
    public ScoreChangedDto Scores { get; init; } = default!;
    public NextQuestionDto? NextQuestion { get; init; }
    public BattleFinishedDto? Finished { get; init; }
    public LastAnswerdQuestionDetail? LastAnswerdQuestionDetail { get; init; }
}

public class BattleCompletionResult
{
    public int BattleStatus { get; set; }
    public int? WinnerId { get; set; }
    public int WinnerGainedXp { get; set; }
    public int LooserGainedXp { get; set; }
    public int User1CorrectedAns { get; set; }
    public int User2CorrectedAns { get; set; }
    public TimeSpan User1TakenTime { get; set; }
    public TimeSpan User2TakenTime { get; set; }
}

public class BattleStartDetails
{
    public int BattleAttemptId { get; set; }
    public string BattleName { get; set; } = null!;
    public PlayerProfileDTO PlayerProfile { get; set; }
    public PlayerProfileDTO OpponentProfile { get; set; }
    public int TotalQuestions { get; set; }
    public int BattleId { get; set; }
}