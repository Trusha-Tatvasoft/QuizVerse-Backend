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
public class QuizManagmentController(IQuizManagmentService quizManagmentService) : ControllerBase
{
    #region Quiz Card Data 
    [HttpPost("get-quiz-card-data")]
    public async Task<IActionResult> GetQuizCardData()
    {
        return Ok(new ApiResponse<QuizManagmentPageDataDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await quizManagmentService.GetQuizCardData()
        });
    }
    #endregion

    #region Get Paginated Quiz List
    [HttpPost("get-quiz-list")]
    public async Task<IActionResult> GetQuizList([FromBody] PageListRequest pageListRequest)
    {
        return Ok(new ApiResponse<PageListResponse<QuizListDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await quizManagmentService.GetQuizzesByPagination(pageListRequest)
        });
    }
    #endregion

}
