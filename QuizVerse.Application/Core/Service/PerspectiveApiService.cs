using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Service;

public class PerspectiveApiService : IPerspectiveApiService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<PerspectiveApiService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public PerspectiveApiService(
           HttpClient httpClient,
           IConfiguration configuration,
           ILogger<PerspectiveApiService> logger,
           IServiceScopeFactory serviceScopeFactory)
    {
        _httpClient = httpClient;
        _apiKey = configuration["PerspectiveApi:ApiKey"];
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<PerspectiveAnalysisResult> AnalyzeTextAsync(GcpApiReportDataDto reportDataDto)
    {

        PerspectiveAnalysisResult result = new PerspectiveAnalysisResult
        {
            ReportId = reportDataDto.ReportId,
            ReportType = reportDataDto.ReportType,
        };

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogError("Perspective API key is not configured.");

            result.IsSuccess = false;
            result.ErrorMessage = "Perspective API key is not configured.";
            return result;
        }

        // Get attributes based on report type
        var attributes = GetAttributesForReportType(reportDataDto.ReportType);
        var requestedAttributes = attributes.ToDictionary(
            attr => attr,
            attr => new { });

        var requestBody = new
        {
            comment = new { text = reportDataDto.ReportComment },
            languages = new[] { "en" },
            requestedAttributes
        };

        string url = $"https://commentanalyzer.googleapis.com/v1alpha1/comments:analyze?key={_apiKey}";
        
        using var scope = _serviceScopeFactory.CreateScope();
        var _aiLogService = scope.ServiceProvider.GetRequiredService<IAiLogService>();

        AiApiCallStartDetail aiApiCallStartDetail = new AiApiCallStartDetail()
        {
            ModelName = (int)AiModelName.PerspectiveApi,
            StartTime = DateTime.UtcNow,
            Purpose = (int)AiApiPurpose.SeverityAnalysis,
        };

        AiProcessLog aiProcessLog = _aiLogService.StartApiCall(aiApiCallStartDetail);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Perspective API error: {StatusCode}, {Content}",
                    response.StatusCode, errorContent);

                _ = _aiLogService.EndApiCall(aiProcessLog, false);

                result.IsSuccess = false;
                result.ErrorMessage = $"API returned {response.StatusCode}, {errorContent}";
                return result;
            }
            _ = _aiLogService.EndApiCall(aiProcessLog, true);

            var apiResponse = await response.Content.ReadFromJsonAsync<PerspectiveApiResponse>();

            List<double> scoresList = [];
            Dictionary<string, double> scoresDict = [];

            // Extract scores
            if (apiResponse?.AttributeScores != null)
            {

                foreach (var attr in attributes)
                {
                    if (apiResponse.AttributeScores.TryGetValue(attr, out var scoreObj))
                    {
                        var score = scoreObj?.SummaryScore?.Value ?? 0.0;
                        scoresList.Add(score);
                        scoresDict[attr] = score;
                    }
                }
            }

            // Calculate max score
            result.AttributeScore = scoresList.Count != 0 ? scoresList.Max() : 0;

            // For Issue Reports: Just set severity
            if (reportDataDto.ReportType == ReportType.QuestionIssueReport || reportDataDto.ReportType == ReportType.QuizIssueReport)
            {
                result.Severity = CalculateSeverity(result.AttributeScore);
            }
            // For QuizRating: Set reason based on highest scoring attribute
            else if (reportDataDto.ReportType == ReportType.QuizRatingFeedback && scoresList.Count != 0)
            {
                // Find the highest scoring attribute
                var highestAttribute = scoresDict.OrderByDescending(x => x.Value).First();
                result.AttributeName = highestAttribute.Key;
                result.AttributeScore = highestAttribute.Value;
                result.IsFlagged = highestAttribute.Value > 0.4;
                result.FlagReason = result.IsFlagged ? GetFlagReasonFromAttribute(highestAttribute.Key) : null;
            }

            result.IsSuccess = true;

            _logger.LogInformation(
                "Analyzed {ReportType} ID: {ReportId}, Comment: {ReportComment} MaxScore: {MaxScore:F3}, Reason: {Reason}",
                reportDataDto.ReportType, reportDataDto.ReportId, reportDataDto.ReportComment, result.AttributeScore, result.FlagReason ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Perspective API for {ReportType} ID: {ReportId}",
                reportDataDto.ReportType, reportDataDto.ReportId);

            _ = _aiLogService.EndApiCall(aiProcessLog, false);
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    // Helper method: Get attributes based on report type
    private static string[] GetAttributesForReportType(ReportType reportType) => reportType switch
    {
        ReportType.QuizRatingFeedback => Constants.QuizRatingGcpAttributes,
        ReportType.QuestionIssueReport or ReportType.QuizIssueReport => Constants.QuizAndQuestionGcpAttributes,
        _ => throw new ArgumentException($"Unknown report type: {reportType}")
    };

    // Helper method: Calculate severity from max score
    private static QuestionOrQuizIssueReportSeverity CalculateSeverity(double score)
    {
        if (score > 0.7)
            return QuestionOrQuizIssueReportSeverity.High;
        else if (score > 0.4)
            return QuestionOrQuizIssueReportSeverity.Medium;
        else
            return QuestionOrQuizIssueReportSeverity.Low;
    }

    // Helper method: Get human-readable reason from attribute name
    private static string GetFlagReasonFromAttribute(string attributeName) =>
    Constants.AttributeReasons.TryGetValue(attributeName, out var reason)
        ? reason
        : "Potentially inappropriate content";
}


