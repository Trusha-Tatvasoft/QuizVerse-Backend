using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = Constants.RoleGroups.Admins)]
[Route("api/[controller]")]
public class ContentModerationController(IContentModerationService _contentModerationService) : ControllerBase
{
    [HttpPost("get-quiz-report-by-pagination")]
    public async Task<IActionResult> GetQuizReportByPagination([FromBody] PageListRequest query)
    {
        return Ok(new ApiResponse<PageListResponse<QuizReportIssueResponseDTO>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await _contentModerationService.GetQuizReportByPaginationAsync(query)
        });
    }

    #region  GetMatricsData
    [HttpGet("get-content-moderation-matrics-data")]
    public async Task<IActionResult> GetContentModerationMatricsData()
    {
        // Placeholder for future implementation
        return Ok(new ApiResponse<ContentModerationMetricsDataDto>
        {
            Result = true,
            Message = "Content moderation metrics data feature is under development.",
            StatusCode = 200,
            Data = await _contentModerationService.GetContentModerationMatricsData()
        });
    }
    #endregion

    #region QuestionReport
    [HttpPost("get-question-report-by-pagination")]
    public async Task<IActionResult> GetQuestionReportByPagination([FromBody] PageListRequest query)
    {
        return Ok(new ApiResponse<PageListResponse<QuestionIssueReportDTO>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await _contentModerationService.GetQuestionReportByPaginationAsync(query)
        });
    }

    [HttpPut("update-question_report_action")]
    public async Task<IActionResult> UpdateQuestionReportAction([FromBody] QuizAndQuestionReportAction actionRequest)
    {
        return Ok(new ApiResponse<string>
        {
            Result = true,
            Message = await _contentModerationService.UpdateQuestionReportAction(actionRequest),
            StatusCode = 200,
            Data = null
        });
    }

    [HttpGet("get_question_issue_report_preview/{queId:int}")]
    public async Task<IActionResult> GetQuestionIssueReportPreview(int queId)
    {
        if (queId <= 0)
            throw new AppException(Constants.INVALID_QUESTION_ID_MESSAGE, StatusCodes.Status400BadRequest);

        // Placeholder for future implementation
        return Ok(new ApiResponse<QuestionIssuePreviewRequestDto>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = await _contentModerationService.GetQuestionIssueReportPreview(queId)
        });
    }

    [HttpGet("get_affected_quiz_and_battle/{queId:int}")]
    public async Task<IActionResult> GetAffectedQuizAndBattle(int queId)
    {
        if (queId <= 0)
            throw new AppException(Constants.INVALID_QUESTION_ID_MESSAGE, StatusCodes.Status400BadRequest);

        // Placeholder for future implementation
        return Ok(new ApiResponse<List<ActiveQuizBattleAffectedDTO>>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = await _contentModerationService.GetAffectedQuizAndBattle(queId)
        });
    }
    #endregion
}
