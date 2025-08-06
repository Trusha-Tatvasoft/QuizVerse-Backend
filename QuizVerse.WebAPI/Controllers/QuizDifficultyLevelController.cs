using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
// [Authorize(Roles = nameof(UserRoles.Admin))]
[Route("api/[controller]")]
public class QuizDifficultyLevelController(IQuizDifficultyLevelService quizDifficultyLevelService) : ControllerBase
{
    [HttpGet("get-quiz-difficulty-list")]
    public async Task<IActionResult> GetQuizDifficultyList()
    {
        return Ok(new ApiResponse<List<QuizDifficultyDTO>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await quizDifficultyLevelService.GetQuizDifficultyList()
        });
    }


    [HttpGet("get-all-quiz-difficulties")]
    public IActionResult GetAllQuizDifficulties()
    {
        ApiResponse<List<CommonListDropDownDto>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = quizDifficultyLevelService.GetAllQuizDifficulties()
        };

        return Ok(response);
    }
}