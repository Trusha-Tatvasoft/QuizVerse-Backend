using AutoMapper;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuizTagsService(IGenericRepository<QuizTag> _quizCategoryRepository, IMapper _mapper) : IQuizTagsService
{
    public List<CommonListDropDownDto> GetAllQuizTags()
    {
        IQueryable<QuizTag> quizTags = _quizCategoryRepository.GetQueryableInclude();
        return _mapper.ProjectTo<CommonListDropDownDto>(quizTags).ToList();
    }
}