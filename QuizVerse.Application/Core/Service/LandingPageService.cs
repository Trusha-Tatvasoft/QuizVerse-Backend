using System.Text.Json;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class LandingPageService : ILandingPageService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Quiz> _quizRepository;
    private readonly IGenericRepository<BaseQuestion> _baseQuestionRepository;
    private readonly IGenericRepository<PlatformConfiguration> _platformConfigurationRepository;

    public LandingPageService(IGenericRepository<User> userRepository, IGenericRepository<Quiz> quizRepository, IGenericRepository<BaseQuestion> baseQuestionRepository, IGenericRepository<PlatformConfiguration> platformConfigurationRepository)
    {
        _userRepository = userRepository;
        _quizRepository = quizRepository;
        _baseQuestionRepository = baseQuestionRepository;
        _platformConfigurationRepository = platformConfigurationRepository;
    }

    public async Task<LandingPageData> GetLandingPageDataAsync()
    {
        long activePlayers = await _userRepository.CountAsync(u => u.Status == (int)UserStatus.Active && u.IsDeleted == false);
        long quizCreated = await _quizRepository.CountAsync(u => u.IsDeleted == false);
        long questionAns = await _baseQuestionRepository.CountAsync(u => u.IsDeleted == false);
        string quote = "Welcome to QuizVerse!";

        PlatformConfiguration? platformConfiguration = await _platformConfigurationRepository.GetAsync(u => u.ConfigurationName == "Platform Quote");

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
