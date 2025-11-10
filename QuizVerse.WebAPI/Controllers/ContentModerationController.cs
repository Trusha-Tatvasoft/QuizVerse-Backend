using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
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
}
