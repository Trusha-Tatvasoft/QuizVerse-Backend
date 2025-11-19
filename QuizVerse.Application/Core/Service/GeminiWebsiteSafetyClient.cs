using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;

public class GeminiWebsiteSafetyClient(
    IConfiguration _config,
    IHttpClientFactory _httpClientFactory,
    IGeminiModelService _modelRotator,
    IAiLogService _aiLogService) : IGeminiWebsiteSafetyClient
{
    private readonly string _apiKey = _config["ApiKeys:GeminiApiKey"] ?? throw new ArgumentNullException(nameof(_apiKey));
    private readonly HttpClient _httpClient = _httpClientFactory.CreateClient();

    public async Task<(bool IsUnsafe, string Message)> IsUnsafeAsync(string url)
    {
        AiModelName modelName = default;

        try
        {
            // Get the next available model
            modelName = await _modelRotator.GetNextAvailableModelAsync();

            // Format the prompt with the URL
            var prompt = string.Format(PromptConstants.CHECK_WEBSITE_CATEGORY_PROMPT, url);

            // Create the request payload for Gemini API
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var modelConfig = Constants.GeminiAIModels.FirstOrDefault(m => m.Name == modelName);

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, Constants.ENCODING_TYPE);

            AiApiCallStartDetail aiApiCallStartDetail = new AiApiCallStartDetail()
            {
                ModelName = (int)modelName,
                StartTime = DateTime.UtcNow,
                Purpose = (int)AiApiPurpose.URLSafetyCheck,
            };

            AiProcessLog aiProcessLog = _aiLogService.StartApiCall(aiApiCallStartDetail);
            HttpResponseMessage response = null;

            try
            {
                response = await _httpClient.PostAsync(
                    string.Format(SystemConstants.GEMINI_API_BASE_URL_FORMAT, modelConfig.ApiEndpoint, _apiKey),
                    content);

                response.EnsureSuccessStatusCode();

                _ = _aiLogService.EndApiCall(aiProcessLog, true);
            }
            catch
            {
                _ = _aiLogService.EndApiCall(aiProcessLog, false);
                throw;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GeminiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Extract the response text
            var responseText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrEmpty(responseText))
            {
                throw new AppException(string.Format(Constants.GEMINI_API_EMPTY_RESPONSE, url), StatusCodes.Status503ServiceUnavailable);
            }

            // Record successful request
            _modelRotator.RecordRequestAsync(modelName);

            // Check if the response indicates an unsafe category
            bool isUnsafe = !responseText.Contains(Constants.WEB_SAFE_MESSAGE_GEMINI, StringComparison.OrdinalIgnoreCase);
            return (isUnsafe, responseText);
        }
        catch (HttpRequestException ex)
        {
            // If this model fails, mark it as unavailable temporarily?
            string modelInfo = modelName == default ? Constants.UNKNOWN_MODEL : modelName.ToString();
            throw new AppException(string.Format(Constants.GEMINI_HTTP_ERROR, url, modelInfo, ex.Message), StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex) when (!(ex is AppException))
        {
            throw new AppException(string.Format(Constants.UNEXPECTED_GEMINI_CHECK_ERROR, url, ex.Message), StatusCodes.Status500InternalServerError);
        }
    }
}
