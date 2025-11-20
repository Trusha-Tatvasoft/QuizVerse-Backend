using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using System.Net.Http.Json;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Service;

public class GroqContentValidatorService(HttpClient http, IConfiguration config, IGroqModelRotationService modelRotation, IAiLogService _aiLogService) : IGroqContentValidatorService
{
    private readonly HttpClient _http = http;
    private readonly IConfiguration _config = config;
    private readonly string _validatorApiKey = config["Groq:ValidatorApiKey"] ?? config["Groq:ApiKey"]!;
    private const int MAX_RETRIES = 3;

    private readonly ConcurrentDictionary<string, int> _inFlightRequests = new();
    private readonly object _requestLock = new();

    public async Task<ContentValidationResult> ValidateContentAsync(string content, string requestedCategory = "educational")
    {
        return await ValidateWithRetryAsync(content, requestedCategory, 0, estimatedTokens: 200);
    }

    private async Task<ContentValidationResult> ValidateWithRetryAsync(
        string content,
        string requestedCategory,
        int retryCount,
        int estimatedTokens = 200)
    {
        if (retryCount >= MAX_RETRIES)
        {
            return new ContentValidationResult
            {
                IsValid = true,
                IsMatch = true,
                Category = "unknown",
                Reason = Constants.MAX_RETRIES_REACHED_DURING_VALIDATION,
                ValidationFailed = true
            };
        }

        try
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _validatorApiKey);

            var modelConfig = GetModelWithThrottling(estimatedTokens);

            if (modelConfig == null)
            {
                await Task.Delay(2000);
                return await ValidateWithRetryAsync(content, requestedCategory, retryCount + 1, estimatedTokens);
            }

            var validationPrompt = BuildValidationPrompt(content, requestedCategory);

            var body = new
            {
                model = modelConfig.ModelName,
                messages = new[]
                {
                    new { role = "system", content = PromptConstants.VALIDATE_AI_SYSYTEM_INSTRUCTIONS },
                    new { role = "user", content = validationPrompt }
                },
                temperature = 0.3,
                max_completion_tokens = 200
            };
            AiApiCallStartDetail aiApiCallStartDetail = new AiApiCallStartDetail()
            {
                ModelName = (int)Constants.GetGroqModelEnumNumber(body.model),
                StartTime = DateTime.UtcNow,
                Purpose = (int)AiApiPurpose.ContentValidation,
            };
            AiProcessLog aiProcessLog = _aiLogService.StartApiCall(aiApiCallStartDetail);

            HttpResponseMessage? response = await _http.PostAsJsonAsync(SystemConstants.GROQ_API_URL, body);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    DecrementInFlightRequest(modelConfig.ModelName);
                    modelRotation.HandleRateLimitExceeded(modelConfig.ModelName);

                    int backoffMs = Math.Min(3000, 300 * (int)Math.Pow(2, retryCount));
                    await Task.Delay(backoffMs);
                    _ = _aiLogService.EndApiCall(aiProcessLog, false);

                    return await ValidateWithRetryAsync(content, requestedCategory, retryCount + 1, estimatedTokens);
                }

                DecrementInFlightRequest(modelConfig.ModelName);

                if ((int)response.StatusCode >= 500)
                {
                    await Task.Delay(1000);
                    _ = _aiLogService.EndApiCall(aiProcessLog, false);
                    return await ValidateWithRetryAsync(content, requestedCategory, retryCount + 1, estimatedTokens);
                }
                _ = _aiLogService.EndApiCall(aiProcessLog, false);
                throw new HttpRequestException($"Validation API error: {response.StatusCode} - {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();

            int tokensUsed = ExtractTokenUsage(json);
            modelRotation.RecordUsage(modelConfig.ModelName, tokensUsed);
            DecrementInFlightRequest(modelConfig.ModelName);

            var result = ParseValidationResponse(json, requestedCategory);
            _ = _aiLogService.EndApiCall(aiProcessLog, true);
            return result;
        }
        catch (HttpRequestException)
        {
            await Task.Delay(1000);
            return await ValidateWithRetryAsync(content, requestedCategory, retryCount + 1, estimatedTokens);
        }
        catch (TaskCanceledException)
        {
            return await ValidateWithRetryAsync(content, requestedCategory, retryCount + 1, estimatedTokens);
        }
        catch (Exception ex)
        {
            return new ContentValidationResult
            {
                IsValid = true,
                IsMatch = true,
                Category = "unknown",
                Reason = $"Validation error: {ex.Message}",
                ValidationFailed = true
            };
        }
    }

    private ModelConfig? GetModelWithThrottling(int estimatedTokens)
    {
        lock (_requestLock)
        {
            var model = modelRotation.GetAvailableModel();

            if (model == null)
                return null;

            int currentInFlight = _inFlightRequests.GetValueOrDefault(model.ModelName, 0);
            int estimatedTotalTokens = model.TokensUsedPerMinute + (currentInFlight * estimatedTokens) + estimatedTokens;

            double safetyThreshold = model.TokensPerMinute * 0.9;

            if (estimatedTotalTokens > safetyThreshold)
            {
                modelRotation.ForceRotateToNextModel();
                model = modelRotation.GetAvailableModel();

                if (model == null)
                    return null;
            }

            _inFlightRequests.AddOrUpdate(model.ModelName, 1, (key, val) => val + 1);

            return model;
        }
    }

    private void DecrementInFlightRequest(string modelName)
    {
        lock (_requestLock)
        {
            _inFlightRequests.AddOrUpdate(modelName, 0, (key, val) => Math.Max(0, val - 1));
        }
    }

    private int ExtractTokenUsage(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("usage", out JsonElement usage))
            {
                if (usage.TryGetProperty("total_tokens", out JsonElement totalTokens))
                {
                    return totalTokens.GetInt32();
                }
            }
        }
        catch (Exception)
        {
            // Fallback to default
        }

        return 200;
    }

    private string BuildValidationPrompt(string content, string requestedCategory)
    {
        return string.Format(
            PromptConstants.VALIDATE_GENERATE_QUESTION_PROMPT,
            requestedCategory, content.Substring(0, Math.Min(content.Length, 2000)),
            content.Length > 2000 ? "..." : ""
        );
    }

    private ContentValidationResult ParseValidationResponse(string jsonResponse, string requestedCategory)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out JsonElement message) &&
                    message.TryGetProperty("content", out JsonElement content))
                {
                    var contentString = content.GetString() ?? "{}";

                    contentString = contentString.Trim();
                    if (contentString.StartsWith("```json"))
                        contentString = contentString.Substring(7);
                    if (contentString.StartsWith("```"))
                        contentString = contentString.Substring(3);
                    if (contentString.EndsWith("```"))
                        contentString = contentString.Substring(0, contentString.Length - 3);
                    contentString = contentString.Trim();

                    using var validationDoc = JsonDocument.Parse(contentString);
                    var validationRoot = validationDoc.RootElement;

                    return new ContentValidationResult
                    {
                        IsValid = validationRoot.TryGetProperty("isValid", out var isValidEl) && isValidEl.GetBoolean(),
                        IsMatch = validationRoot.TryGetProperty("isMatch", out var isMatchEl) && isMatchEl.GetBoolean(),
                        Category = validationRoot.TryGetProperty("detectedCategory", out var catEl) ? catEl.GetString() ?? "unknown" : "unknown",
                        Reason = validationRoot.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString() ?? "" : "",
                        RequestedCategory = requestedCategory,
                        ValidationFailed = false
                    };
                }
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{Constants.FAILED_TO_PASRE_JSON_RESPONSE} {ex.Message}", ex);
        }

        return new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "unknown",
            Reason = Constants.FAILED_TO_PASRE_JSON_RESPONSE,
            RequestedCategory = requestedCategory,
            ValidationFailed = true
        };
    }
}