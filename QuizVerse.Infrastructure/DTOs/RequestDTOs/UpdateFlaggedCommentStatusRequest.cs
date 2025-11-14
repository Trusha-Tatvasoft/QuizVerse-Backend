using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class UpdateFlaggedCommentStatusRequest
{
    [Required]
    public int Id { get; set; }

    [Range(1, 2, ErrorMessage = "Status must be either 1 (Accepted) or 2 (Ignored).")]
    public int Status { get; set; }
}
