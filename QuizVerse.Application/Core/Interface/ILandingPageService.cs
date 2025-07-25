using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface ILandingPageService
{
    Task<LandingPageData> GetLandingPageData();
}
