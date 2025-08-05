namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class PageListResponse<T>
{
    public int TotalRecords { get; set; }
    public List<T> Records { get; set; } = [];
}
