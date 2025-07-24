using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.Application.Core.Interface;

public interface ILandingPageService
{
    public Task<LandingPageData> GetLandingPageDataAsync();
}
