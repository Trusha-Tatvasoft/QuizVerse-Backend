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
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRoles.Admin))]
public class EmailTemplatesController() : ControllerBase
{
    [HttpGet("get-all-email-templates")]
    public IActionResult GetAllEmailTemplates([FromServices] IEmailTemplatesService emailTemplatesService, [FromQuery] PageListRequest pageListRequest)
    {
        var response = emailTemplatesService.GetAllEmailTemplates(pageListRequest);
        
        return Ok(new ApiResponse<PageListResponse<EmailTemplatesResponseDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = emailTemplatesService.GetAllEmailTemplates(pageListRequest)
        });
    }
}