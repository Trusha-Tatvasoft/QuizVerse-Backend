using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = nameof(UserRoles.Admin) + "," + nameof(UserRoles.SuperAdmin))]
    public class QuizController(IQuizService _quizService) : ControllerBase
    {
        [HttpGet("get-quiz-overview/{quizId}")]
        public async Task<IActionResult> GetQuizOverview(int quizId)
        {
            return Ok(new ApiResponse<QuizOverviewResponseDto>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.FETCH_DATA_MESSAGE,
                Data = await _quizService.GetQuizOverviewAsync(quizId)
            });
        }

        [HttpPost("start-quiz/{quizId}")]
        public async Task<IActionResult> StartQuiz(int quizId)
        {
            return Ok(new ApiResponse<QuizStartResponseDto>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.FETCH_DATA_MESSAGE,
                Data = await _quizService.StartQuizAsync(quizId)
            });
        }

        // save and next 
        [HttpPost("save-and-next-question")]
        public async Task<IActionResult> SaveAndNextQuestion([FromBody] SaveAndNextQuestionRequestDto request)
        {
            // Call service directly, let exceptions bubble up
            return Ok(new ApiResponse<QuizQuestionResponseDto>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Answer saved and next question fetched successfully",
                Data = await _quizService.SaveAndNextQuestion(request)
            });
        }

        [HttpPost("submit-quiz")]
        public async Task<IActionResult> SubmitQuiz([FromBody] SubmitQuizRequestDTO request)
        {
            return Ok(new ApiResponse<bool>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.QUIZ_SUBMITTED_SUCCESSFULLY,
                Data = await _quizService.SubmitQuiz(request)
            });
        }

        [HttpGet("quiz-summary/{quizId:int}")]
        public async Task<IActionResult> GetQuizSummary(int quizId)
        {
            return Ok(new ApiResponse<QuizCompletedSummaryDTO>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.QUIZ_COMPLETED_SUMMARY_FETCHED,
                Data = await _quizService.GetQuizSummary(quizId)
            });
        }

        [HttpGet("quiz-question-review/{quizId:int}")]
        public async Task<IActionResult> GetQuizQuestionReview(int quizId)
        {
            return Ok(new ApiResponse<List<QuizQuestionReviewDTO>>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.QUIZ_QUESTION_REVIEW_FETCHED,
                Data = await _quizService.GetQuizQuestionReview(quizId)
            });
        }

        [HttpPost("report-question-issue")]
        public async Task<IActionResult> ReportQuestionIssue([FromBody] QuestionIssueReportRequestDTO request)
        {
            return Ok(new ApiResponse<QuizCompletedSummaryDTO>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = await _quizService.ReportQuestionIssue(request),
                Data = null
            });
        }

        [HttpGet("quiz-rating/{quizId:int}")]
        public async Task<IActionResult> GetMyQuizRating(int quizId)
        {
            QuizRatingDTO? rating = await _quizService.GetMyQuizRating(quizId);

            return Ok(new ApiResponse<QuizRatingDTO?>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = rating == null ? Constants.QUIZ_RATING_NOT_FOUND : Constants.QUIZ_RATING_FETCHED,
                Data = rating
            });
        }

        [HttpPost("submit-quiz-rating")]
        public async Task<IActionResult> SubmitQuizRating([FromBody] QuizRatingDTO request)
        {
            return Ok(new ApiResponse<string>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = await _quizService.SubmitQuizRating(request),
                Data = null
            });
        }

        [HttpPost("get-answer-explanation")]
        public async Task<IActionResult> GetAnswerExplanation([FromBody] AnswerExplanationRequestDTO request)
        {
            return Ok(new ApiResponse<string>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.QUIZ_ANSWER_EXPLANATION_GENERATED,
                Data = await _quizService.GetAnswerExplanation(request),
            });
        }
    }
}
