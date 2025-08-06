using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizTagsService
{
    List<CommonListDropDownDto> GetAllQuizTags();
}