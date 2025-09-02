using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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
}