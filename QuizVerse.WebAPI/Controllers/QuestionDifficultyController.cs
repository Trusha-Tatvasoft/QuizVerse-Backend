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
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRoles.Admin))]
public class QuestionDifficultyController(IQuestionDifficultyService questionDifficultyService) : ControllerBase
{
    #region Get Battle Question Data 
    [HttpGet("get-battle-question-difficulty-data")]
    public async Task<IActionResult> GetBattleQuestionDifficultyData()
    {
        return Ok(new ApiResponse<List<QuestionDifficultyXPData>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await questionDifficultyService.GetBattleQuestionDifficultyData()
        });
    }
    #endregion

    [HttpGet("get-question-difficulties")]
    public IActionResult GetQuestionDifficulties()
    {
        return Ok(new ApiResponse<List<QuestionDifficultyResponseDTO>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = questionDifficultyService.GetQuestionDifficulties()
        });
    }

    [HttpPost("add-or-edit-question-difficulty")]
    public async Task<IActionResult> AddOrEditQuestionDifficulty(QuestionDifficultyRequestDTO request)
    {
        return Ok(new ApiResponse<object>
        {
            Result = true,
            Message = await questionDifficultyService.AddOrEditQuestionDifficulty(request),
            StatusCode = 200,
            Data = null
        });
    }

    [HttpDelete("delete-question-difficulty")]
    public async Task<IActionResult> DeleteQuestionDifficulty(int questionDifficultiesId)
    {
        return Ok(new ApiResponse<object>
        {
            Result = true,
            Message = await questionDifficultyService.DeleteQuestionDifficulty(questionDifficultiesId),
            StatusCode = 200,
            Data = null
        });
    }

    [HttpGet("is-question-difficulty-name-available/{name}")]
    public async Task<IActionResult> IsQuestionDifficultyNameAvailable(string name)
    {
        return Ok(new ApiResponse<string>
        {
            Result = await questionDifficultyService.IsQuestionDifficultyNameAvailable(name),
            Message = Constants.VALID_DATA,
            StatusCode = 200,
            Data = null
        });
    }

    [HttpGet("is-question-difficulty-xp-available/{xp}")]
    public async Task<IActionResult> IsQuestionDifficultyXPAvailable(int xp)
    {
        return Ok(new ApiResponse<string>
        {
            Result = await questionDifficultyService.IsQuestionDifficultyXPAvailable(xp),
            Message = Constants.VALID_DATA,
            StatusCode = 200,
            Data = null
        });
    }
}