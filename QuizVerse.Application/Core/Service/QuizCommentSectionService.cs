using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuizCommentSectionService(IGenericRepository<QuizRating> _quizRatingRepository, IGenericRepository<Quiz> _quizRepository) : IQuizCommentSectionService
{
    public async Task<List<QuizCommentsDto>> GetCommentsByQuizId(int quizId)
    {
        bool exists = await _quizRepository.Exists(q => q.Id == quizId && q.IsDeleted == false);

        if (!exists)
        {
            throw new AppException(Constants.QUIZ_NOT_FOUND);
        }

        List<QuizCommentsDto> comments = _quizRatingRepository.GetQueryableInclude().Where(q => q.QuizId == quizId && q.IsFlagged == false && q.User.IsDeleted == false).Include(q => q.User)
            .Select(q => new QuizCommentsDto
            {
                UserName = q.User!.UserName,
                ProfilePic = q.User.ProfilePic,
                CommentDate = q.CreatedDate,
                CommentText = q.Feedback ?? "",
                Rating = q.QuizRating1
            }).ToList();
            
        return comments;
    }
}