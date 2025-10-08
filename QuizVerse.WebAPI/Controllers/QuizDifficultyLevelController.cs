using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Constants.RoleGroups.Admins))]
[Route("api/[controller]")]
public class QuizDifficultyLevelController(IQuizDifficultyLevelService quizDifficultyLevelService) : ControllerBase
{
    #region Get List
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
    #endregion

    #region Get By Id
    [HttpGet("get-difficulty-level-by-id/{id}")]
    public async Task<IActionResult> GetDifficultyLevelById(int id)
    {
        return Ok(new ApiResponse<QuizDifficultyDTO>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await quizDifficultyLevelService.GetDifficultyLevelById(id)
        });
    }
    #endregion

    #region Difficulty Name Available
    [HttpGet("is-difficulty-name-available/{name}")]
    public async Task<IActionResult> IsDifficultyNameAvailable(string name)
    {
        return Ok(new ApiResponse<string>
        {
            Result = await quizDifficultyLevelService.IsDifficultyNameAvailable(name),
            Message = Constants.VALID_DATA,
            StatusCode = 200,
            Data = null
        });
    }
    #endregion

    #region Create Difficulty Level
    [HttpPost("create-difficulty-level")]
    public async Task<IActionResult> CreateDifficultyLevel([FromBody] QuizDifficultyRequestDto difficultyRequestDto)
    {
        return Ok(new ApiResponse<object>
        {
            Result = true,
            Message = await quizDifficultyLevelService.CreateDifficultyLevel(difficultyRequestDto),
            StatusCode = 200,
            Data = null
        });
    }
    #endregion
}