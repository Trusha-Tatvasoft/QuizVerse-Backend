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

    [HttpPut("update-quiz-report-action")]
    public async Task<IActionResult> UpdateQuizReportAction([FromBody] QuizAndQuestionReportAction actionRequest)
    {
        return Ok(new ApiResponse<string>
        {
            Result = true,
            Message = await _contentModerationService.UpdateQuizReportAction(actionRequest),
            StatusCode = 200,
            Data = null
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

    #region FlaggedComments
    [HttpPost("get-flagged-comments")]
    public async Task<IActionResult> GetFlaggedComments([FromBody] PageListRequest request)
    {
        return Ok(new ApiResponse<PageListResponse<FlaggedCommentDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await _contentModerationService.GetFlaggedComments(request)
        });
    }

    [HttpGet("get-flagged-comment-by-id/{id}")]
    public async Task<IActionResult> GetFlaggedCommentById(int id)
    {
        return Ok(new ApiResponse<FlaggedCommentViewDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await _contentModerationService.GetFlaggedCommentById(id)
        });
    }

    [HttpPut("update-flagged-comment-status")]
    public async Task<IActionResult> UpdateFlaggedCommentStatus([FromBody] UpdateFlaggedCommentStatusRequest request)
    {
        await _contentModerationService.UpdateFlaggedCommentStatus(request);

        return Ok(new ApiResponse<object>
        {
            Result = true,
            Message = Constants.UPDATE_SUCCESS,
            StatusCode = 200,
            Data = null
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

    [HttpPost("update-reported-question/{reportId:int}")]
    public async Task<IActionResult> UpdateReportedQuestion(int reportId, [FromBody] QuestionRequestDTO dto)
    {
        ApiResponse<object> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = await _contentModerationService.UpdateReportedQuestion(reportId, dto),
            Data = null
        };

        return Ok(response);
    }
    #endregion
}
