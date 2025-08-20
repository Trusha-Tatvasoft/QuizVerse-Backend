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
[Authorize(Roles = nameof(UserRoles.Admin))]
[Route("api/[controller]")]
public class QuizManagementController(IQuizManagementService quizManagementService) : ControllerBase
{
    #region Quiz Card Data 
    [HttpGet("get-quiz-card-data")]
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
    public async Task<IActionResult> CreateUpdateQuiz([FromBody] SaveQuizRequestDto quizCreateUpdateRequestDto)
    {
        CreateUpdateResponseDto response = await quizManagementService.CreateUpdateQuiz(quizCreateUpdateRequestDto);
        return Ok(new ApiResponse<CreateUpdateResponseDto>
        {
            Result = response.Success,
            Message = response.Message,
            StatusCode = StatusCodes.Status200OK,
            Data = null
        });
    }
    #endregion

    #region Get Quiz By Id
    [HttpGet("get-quiz-by-id/{quizId}")]
    public async Task<IActionResult> GetQuizById(int quizId)
    {
        return Ok(new ApiResponse<QuizResponseDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await quizManagementService.GetQuizDataById(quizId)
        });
    }
    #endregion

    #region Delete Quiz
    [HttpDelete("delete-quiz/{quizId}")]
    public async Task<IActionResult> DeleteQuiz(int quizId)
    {
        CreateUpdateResponseDto response = await quizManagementService.DeleteQuiz(quizId);
        return Ok(new ApiResponse<CreateUpdateResponseDto>
        {
            Result = response.Success,
            Message = response.Message,
            StatusCode = StatusCodes.Status200OK,
            Data = null
        });
    }
    #endregion

    #region Export Questions to CSV
    [HttpPost("export-questions-to-csv")]
    public async Task<IActionResult> ExportQuestionsToCsv([FromBody] ExportQuizQuestionsRequestDto exportRequest)
    {
        var csvContent = await quizManagementService.ExportQuestionsToCsv(exportRequest);
        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", $"{exportRequest.QuizName}_questions.csv");
    }
    #endregion
}
