using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuizCommentSectionService(
    IGenericRepository<QuizRating> _quizRatingRepository,
    IGenericRepository<Quiz> _quizRepository,
    IHttpContextAccessor _httpContextAccessor) : IQuizCommentSectionService
{
    public int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
    public async Task<int> TotalCommentsByQuizId(int quizId)
    {
        if (quizId < 0)
        {
            throw new AppException(Constants.INVALID_QUIZ_ID);
        }

        bool exists = await _quizRepository.Exists(q => q.Id == quizId && q.IsDeleted == false);

        if (!exists)
        {
            throw new AppException(Constants.QUIZ_NOT_FOUND);
        }

        return _quizRatingRepository
            .GetQueryableInclude()
            .Where(q => q.QuizId == quizId && q.IsFlagged == false && q.User.IsDeleted == false)
            .Count();
    }

    public async Task<QuizCommentsResponseDto> GetCommentsByQuizId(int quizId, int batchNumber)
    {
        if (quizId < 0)
        {
            throw new AppException(Constants.INVALID_QUIZ_ID);
        }
        
        if (batchNumber < 1)
        {
            throw new AppException(Constants.WRONG_BATCH_NUMBER);
        }

        bool exists = await _quizRepository.Exists(q => q.Id == quizId && q.IsDeleted == false);

        if (!exists)
        {
            throw new AppException(Constants.QUIZ_NOT_FOUND);
        }

        int pageSize = 4;
        int skipCount = (batchNumber - 1) * pageSize;

        List<QuizCommentsDto> comments = _quizRatingRepository
            .GetQueryableInclude()
            .Where(q => q.QuizId == quizId && q.IsFlagged == false && q.User.IsDeleted == false)
            .Include(q => q.User)
            .OrderByDescending(q => q.CreatedDate)
            .Skip(skipCount)
            .Take(pageSize)
            .Select(q => new QuizCommentsDto
            {
                UserName = q.User!.UserName,
                FullName = q.User!.FullName,
                ProfilePic = q.User.ProfilePic,
                CommentDate = q.CreatedDate,
                CommentText = q.Feedback ?? "",
                Rating = q.QuizRating1,
                isUser = q.UserId == UserId
            }).ToList();

        bool hasMoreComments = _quizRatingRepository
            .GetQueryableInclude()
            .Where(q => q.QuizId == quizId && q.IsFlagged == false && q.User.IsDeleted == false)
            .OrderByDescending(q => q.CreatedDate)
            .Skip(skipCount + pageSize)
            .Any();

        QuizCommentsResponseDto commentsResponse = new()
        {
            Comments = comments,
            hasMoreComments = hasMoreComments
        };

        return commentsResponse;
    }
}