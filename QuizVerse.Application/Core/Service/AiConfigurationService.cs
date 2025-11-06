using System.Text.Json;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class AiConfigurationService(IGenericRepository<AiProcessLog> _aiProcessLogRepository) : IAiConfigurationService
{
    public async Task<AiConfigurationCardDetailsDTO> GetAiConfigurationCardDetails()
    {
        var now = DateTime.UtcNow;
        int currentMonth = now.Month;
        int currentYear = now.Year;

        // Handle last month edge case (January → December of previous year)
        int lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        int lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        // --- Get all QuestionGeneration logs for current month ---
        List<AiProcessLog> aiProcessLogs = await _aiProcessLogRepository.FindAsync(
            log => log.Purpose == (int)AiApiPurpose.QuestionGeneration &&
                   log.StartTime.Month == currentMonth &&
                   log.StartTime.Year == currentYear
        );

        int generatedQuestionsCurrentMonth = 0;

        foreach (var log in aiProcessLogs)
        {
            if (string.IsNullOrWhiteSpace(log.ExtraInfo))
                continue;

            try
            {
                // Parse JSON safely
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(log.ExtraInfo);
                if (json != null && json.TryGetValue(Constants.GENERATED_QUESTIONS_COUNT_JSON_KEY, out var countValue) &&
                    int.TryParse(countValue?.ToString(), out int count))
                {
                    generatedQuestionsCurrentMonth += count;
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        // --- Count API calls ---
        var currentMonthApiCalls = await _aiProcessLogRepository.CountAsync(
            log => log.StartTime.Month == currentMonth && log.StartTime.Year == currentYear
        );

        var lastMonthGeneratedQuestions = await _aiProcessLogRepository.CountAsync(
            log => log.StartTime.Month == lastMonth && log.StartTime.Year == lastMonthYear
        );

        // --- Calculate Success Rate ---
        var totalApiCalls = await _aiProcessLogRepository.CountAsync();
        var successApiCalls = await _aiProcessLogRepository.CountAsync(log => log.IsSuccess);

        decimal successRate = totalApiCalls > 0
            ? Math.Round((decimal)successApiCalls / totalApiCalls * 100, 2)
            : 0;

        // --- Map to DTO ---
        return new AiConfigurationCardDetailsDTO
        {
            CurruntMonthApiCalls = currentMonthApiCalls,
            GeneratedQuestionsCurruntMonth = generatedQuestionsCurrentMonth,
            GeneratedQuestionsLastMonth = lastMonthGeneratedQuestions,
            SuccessRate = successRate
        };
    }

    public async Task<AiUsesDetailsDTO> GetAiUsesDetails(AiModelName? aiModelName)
    {
        var now = DateTime.UtcNow;
        DateTime startOfToday = now.Date;
        DateTime startOfTomorrow = startOfToday.AddDays(1);

        // --- Today's API Calls (UTC day) ---
        int todaysApiCalls = await _aiProcessLogRepository.CountAsync(
            log => log.StartTime >= startOfToday && log.StartTime < startOfTomorrow && (aiModelName == null || log.ModelName == (int)aiModelName)
        );

        // --- Get all logs once ---
        var allLogs = await _aiProcessLogRepository.FindAsync(log => (aiModelName == null || log.ModelName == (int)aiModelName));

        // --- Average Response Time ---
        var logsWithEndTime = allLogs.Where(log => log.EndTime != default(DateTime)).ToList();
        decimal averageResponseTimeInSeconds = logsWithEndTime.Any()
            ? Math.Round((decimal)logsWithEndTime
                .Average(log => (log.EndTime - log.StartTime).TotalSeconds), 2)
            : 0m;

        // --- Error Rate ---
        int totalApiCalls = allLogs.Count;
        int failedApiCalls = allLogs.Count(log => !log.IsSuccess);

        decimal errorRate = totalApiCalls > 0
            ? Math.Round((decimal)failedApiCalls / totalApiCalls * 100, 2)
            : 0m;

        return new AiUsesDetailsDTO
        {
            TodaysApiCalls = todaysApiCalls,
            AverageResponseTimeInSecond = averageResponseTimeInSeconds,
            ErrorRate = errorRate
        };
    }
}
