using System.Text.Json.Serialization;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class BrowseQuizzesResponseDTO
{
    public List<BrowseQuizz> Quizzes { get; set; } = new List<BrowseQuizz>();

    public bool HasMore { get; set; }

    public int TotalFeatured { get; set; }

    public int TotalFree { get; set; }

    public int TotalPremium { get; set; }

    public int TotalAll { get; set; }
}

public class BrowseQuizz
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isPaid")]
    public bool IsPaid { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("difficultyLevel")]
    public string DifficultyLevel { get; set; } = string.Empty;

    [JsonPropertyName("isFeatured")]
    public bool IsFeatured { get; set; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    [JsonPropertyName("totalTime")]
    public decimal TotalTime { get; set; }

    [JsonPropertyName("totalQuestions")]
    public int TotalQuestions { get; set; }

    [JsonPropertyName("totalParticipates")]
    public long TotalParticipates { get; set; }

    [JsonPropertyName("rating")]
    public decimal Rating { get; set; }
}