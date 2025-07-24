using System.Text.Json;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class LandingPageService(
    IGenericRepository<User> userRepository,
    IGenericRepository<Quiz> quizRepository,
    IGenericRepository<BaseQuestion> baseQuestionRepository,
    IGenericRepository<PlatformConfiguration> platformConfigurationRepository
) : ILandingPageService
{
    public async Task<LandingPageData> GetLandingPageData()
    {
        long activePlayers = await userRepository.CountAsync(u => u.Status == (int)UserStatus.Active && u.IsDeleted == false);
        long quizCreated = await quizRepository.CountAsync(u => u.IsDeleted == false);
        long questionAns = await baseQuestionRepository.CountAsync(u => u.IsDeleted == false);
        string quote = Constants.QUIZVERSE_DEFAULT_QUOTE;

        PlatformConfiguration? platformConfiguration = await platformConfigurationRepository.GetAsync(u => u.ConfigurationName == "Platform Quote");

        if (!string.IsNullOrWhiteSpace(platformConfiguration?.Values))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(platformConfiguration.Values);
                if (doc.RootElement.TryGetProperty("PlatformQuote", out JsonElement quoteElement))
                {
                    quote = quoteElement.GetString()! ?? quote;
                }
            }
            catch
            {
                // If parsing fails, quote = "Welcome to QuizVerse!";
            }
        }

        return new LandingPageData
        {
            Quote = quote,
            ActivePlayer = activePlayers,
            QuizCreated = quizCreated,
            QuestionAns = questionAns
        };
    }
}
