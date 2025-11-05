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
}
