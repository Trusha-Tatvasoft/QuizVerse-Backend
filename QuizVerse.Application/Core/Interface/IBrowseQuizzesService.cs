using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IBrowseQuizzesService
{
    Task<BrowseQuizzesResponseDTO> BrowseQuizzes(BrowseQuizzesRequestDTO request);
}
