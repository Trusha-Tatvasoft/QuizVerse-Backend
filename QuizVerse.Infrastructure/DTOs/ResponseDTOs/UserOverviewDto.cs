using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class RecentActivityDto
{
    public string Type { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Xp { get; set; }
}

public class UserOverviewDto
{
    public string RecentActivityJson { get; set; } = null!;
    public int GlobalRank { get; set; }
    public string BestCategory { get; set; } = null!;
    public int LongestStreak { get; set; }

    [NotMapped]
    public List<RecentActivityDto> RecentActivity =>
    string.IsNullOrWhiteSpace(RecentActivityJson)
       ? []
       : JsonSerializer.Deserialize<List<RecentActivityDto>>(
           RecentActivityJson,
           new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
         )!;
}
