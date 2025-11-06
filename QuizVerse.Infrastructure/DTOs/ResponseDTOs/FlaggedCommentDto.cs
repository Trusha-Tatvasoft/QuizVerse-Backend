namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class FlaggedCommentDto
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public string Author { get; set; } = string.Empty;
    public string QuizName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Date { get; set; }
    public int Status { get; set; }
    public int? ModifiedBy { get; set; }
}

public class FlaggedCommentsResultDTO
{
    public string? Records { get; set; }

    public int TotalRecords { get; set; }
}

public class FlaggedCommentViewDto : FlaggedCommentDto
{
    public string QuizCategory { get; set; } = string.Empty;
    public int UserId { get; set; }
}