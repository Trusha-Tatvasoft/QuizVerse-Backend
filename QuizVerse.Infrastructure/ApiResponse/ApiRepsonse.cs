namespace QuizVerse.Infrastructure.ApiResponse;

public class ApiResponse<T>
{
    public bool Result { get; set; }
    public string Message { get; set; } = default!;
    public int StatusCode { get; set; }
    public T? Data { get; set; }
}
