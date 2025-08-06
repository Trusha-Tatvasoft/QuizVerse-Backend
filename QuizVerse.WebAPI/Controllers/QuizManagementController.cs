using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizManagementController(IQuizManagementService quizManagementService) : ControllerBase
{
    #region Quiz Card Data 
    [HttpPost("get-quiz-card-data")]
    public async Task<IActionResult> GetQuizCardData()
    {
        return Ok(new ApiResponse<QuizManagementPageDataDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await quizManagementService.GetQuizCardData()
        });
    }
    #endregion

    #region Get Paginated Quiz List
    [HttpPost("get-quizzes-by-pagination")]
    public async Task<IActionResult> GetQuizzesByPagination([FromBody] PageListRequest pageListRequest)
    {
        return Ok(new ApiResponse<PageListResponse<QuizListDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await quizManagementService.GetQuizzesByPagination(pageListRequest)
        });
    }
    #endregion

}
