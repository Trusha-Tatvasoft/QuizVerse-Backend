using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuestionPoolController(IQuestionPoolService _questionPoolService) : ControllerBase
{
    [HttpGet("get-question-pool-list")]
    public async Task<IActionResult> GetQuestionPoolListAsync([FromQuery] PageListRequest pageListRequest)
    {
        ApiResponse<PageListResponse<QuestionPoolListDto>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = await _questionPoolService.GetQuestionPoolListAsync(pageListRequest)
        };
        return Ok(response);
    }
}