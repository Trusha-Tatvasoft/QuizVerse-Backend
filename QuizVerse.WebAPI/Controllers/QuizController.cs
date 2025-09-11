using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
    }
}
