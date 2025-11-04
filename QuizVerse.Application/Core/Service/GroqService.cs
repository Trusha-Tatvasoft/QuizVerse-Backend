using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using QuizVerse.Application.Core.Interface;
using Microsoft.Extensions.Configuration;
using QuizVerse.Infrastructure.Common;

namespace QuizVerse.Application.Core.Service;

public class GroqService(HttpClient http, IConfiguration _config, IGroqModelRotationService modelRotation) : IGroqService
{
    private const int MAX_RETRIES = 5;

    private readonly ConcurrentDictionary<string, int> _inFlightRequests = new();
    private readonly object _requestLock = new();

    public async Task<string> GenerateQuesions(string prompt)
    {
        return await GenerateQuizWithRetryAsync(prompt, 0, estimatedTokens: 1500);
    }

    private async Task<string> GenerateQuizWithRetryAsync(string prompt, int retryCount, int estimatedTokens = 1500)
    {
        if (retryCount >= MAX_RETRIES)
        {
            throw new InvalidOperationException(string.Format(Constants.MAX_RETRIES_REACH, $"{MAX_RETRIES}"));
        }

        var apiKey = _config["Groq:ApiKey"];
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var modelConfig = GetModelWithThrottling(estimatedTokens);

        if (modelConfig == null)
        {
            await Task.Delay(2000);
            return await GenerateQuizWithRetryAsync(prompt, retryCount + 1, estimatedTokens);
        }

        var body = new
        {
            model = modelConfig.ModelName,
            messages = new[]
            {
                new { role = "system", content = PromptConstants.GENERATE_AI_SYATEM_INSTRUCTIONS },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_completion_tokens = 1500
        };

        try
        {
            var response = await http.PostAsJsonAsync(SystemConstants.GROQ_API_URL, body);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    modelRotation.HandleRateLimitExceeded(modelConfig.ModelName);

                    int backoffMs = Math.Min(5000, 500 * (int)Math.Pow(2, retryCount));
                    await Task.Delay(backoffMs);

                    return await GenerateQuizWithRetryAsync(prompt, retryCount + 1, estimatedTokens);
                }

                if ((int)response.StatusCode >= 500)
                {
                    await Task.Delay(1000);
                    return await GenerateQuizWithRetryAsync(prompt, retryCount + 1, estimatedTokens);
                }

                throw new HttpRequestException($"API error: {response.StatusCode} - {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();

            int tokensUsed = ExtractTokenUsage(json);

            modelRotation.RecordUsage(modelConfig.ModelName, tokensUsed);
            DecrementInFlightRequest(modelConfig.ModelName);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out JsonElement message) &&
                    message.TryGetProperty("content", out JsonElement content))
                {
                    var contentString = content.GetString() ?? "";
                    contentString = CleanJsonResponse(contentString);

                    if (string.IsNullOrWhiteSpace(contentString) || contentString == "[]")
                    {
                        return await GenerateQuizWithRetryAsync(prompt, retryCount + 1, estimatedTokens);
                    }

                    return contentString;
                }
            }

            throw new InvalidOperationException(Constants.QUESTION_GENERATION_FAILED);
        }
        catch (HttpRequestException)
        {
            DecrementInFlightRequest(modelConfig.ModelName);
            await Task.Delay(1000);
            return await GenerateQuizWithRetryAsync(prompt, retryCount + 1, estimatedTokens);
        }
        catch (TaskCanceledException)
        {
            DecrementInFlightRequest(modelConfig.ModelName);
            return await GenerateQuizWithRetryAsync(prompt, retryCount + 1, estimatedTokens);
        }
        catch (Exception ex)
        {
            DecrementInFlightRequest(modelConfig.ModelName);
            throw new InvalidOperationException($"{Constants.GENERATION_FAILED} {ex.Message}", ex);
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
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        var tokensUsed = 0;
        if (root.TryGetProperty("usage", out JsonElement usage))
        {
            if (usage.TryGetProperty("total_tokens", out JsonElement totalTokens))
            {
                tokensUsed = totalTokens.GetInt32();
            }
        }
        return tokensUsed;
    }

    private string CleanJsonResponse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return "[]";

        rawJson = rawJson.Trim();
        if (rawJson.StartsWith("```json"))
            rawJson = rawJson.Substring(7);
        if (rawJson.StartsWith("```"))
            rawJson = rawJson.Substring(3);
        if (rawJson.EndsWith("```"))
            rawJson = rawJson.Substring(0, rawJson.Length - 3);

        rawJson = rawJson.Trim();

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                if (root.GetArrayLength() > 0)
                {
                    var firstElement = root[0];

                    if (firstElement.ValueKind == JsonValueKind.Object &&
                        firstElement.EnumerateObject().Any() &&
                        (firstElement.TryGetProperty("Question", out _) ||
                         firstElement.TryGetProperty("question", out _) ||
                         firstElement.TryGetProperty("text", out _)))
                    {
                        return rawJson;
                    }

                    if (root.GetArrayLength() > 1)
                    {
                        for (int i = 0; i < root.GetArrayLength(); i++)
                        {
                            var element = root[i];
                            if (element.ValueKind == JsonValueKind.Array)
                            {
                                return element.GetRawText();
                            }
                        }
                    }
                }

                return rawJson;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                string[] possibleArrayProps = ["questions", "quiz", "data", "items", "results", "quizzes"];

                foreach (var prop in possibleArrayProps)
                {
                    if (root.TryGetProperty(prop, out JsonElement arrayElement) &&
                        arrayElement.ValueKind == JsonValueKind.Array)
                    {
                        return arrayElement.GetRawText();
                    }
                }

                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        return property.Value.GetRawText();
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            var match = Regex.Match(rawJson, @"\[\s*\{[\s\S]*?\}\s*\]", RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Value;
            }

            throw new JsonException($"{Constants.FAILED_TO_CLEAN_JSON} {ex.Message}", ex);
        }

        return rawJson;
    }
}