namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class PagedResultDto<T>
{
    public int TotalRecords { get; set; }
    public IEnumerable<T> Records { get; set; } = [];
}
