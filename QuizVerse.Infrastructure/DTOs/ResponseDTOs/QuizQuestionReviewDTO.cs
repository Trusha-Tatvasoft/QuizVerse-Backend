using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

[Keyless]
public class QuizQuestionReviewDTO
{
    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("question_text")]
    public string QuestionText { get; set; } = null!;

    [Column("user_answer")]
    public string? UserAnswer { get; set; }

    [Column("correct_answer")]
    public string CorrectAnswer { get; set; } = null!;

    [Column("is_correct")]
    public bool? IsCorrect { get; set; }
}
