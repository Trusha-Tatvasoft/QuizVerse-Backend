using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
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

    #region Create/Update Quiz
    [HttpPost("create-update-quiz")]
    public async Task<IActionResult> CreateUpdateQuiz([FromBody] QuizCreateUpdateRequestDto quizCreationRequestDto)
    {
        var response = await quizManagementService.CreateUpdateQuiz(quizCreationRequestDto);
        return Ok(new ApiResponse<CreateUpdateResponseDto>
        {
            Result = response.Success,
            Message = response.Message,
            StatusCode = response.Success ? 200 : 400,
            Data = null
        });
    }
    #endregion

    #region Get Quiz By Id
    [HttpGet("get-quiz-by-id/{quizId}")]
    public async Task<IActionResult> GetQuizById(int quizId)
    {
        QuizDataResponseDto response = await quizManagementService.GetQuizDataById(quizId);
        return Ok(new ApiResponse<QuizDataResponseDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = response
        });
    }
    #endregion
}
