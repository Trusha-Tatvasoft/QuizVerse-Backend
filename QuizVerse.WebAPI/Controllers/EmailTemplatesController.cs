using System.Threading.Tasks;
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
[Authorize(Roles = Constants.RoleGroups.Admins)]
public class EmailTemplatesController(IEmailTemplatesService emailTemplatesService) : ControllerBase
{
    [HttpPost("get-all-email-templates")]
    public IActionResult GetAllEmailTemplates(PageListRequest pageListRequest)
    {
        return Ok(new ApiResponse<PageListResponse<EmailTemplatesResponseDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = emailTemplatesService.GetAllEmailTemplates(pageListRequest)
        });
    }

    [HttpPost("add-or-edit-email-template")]
    public async Task<IActionResult> AddOrEditEmailTemplate(EmailTemplatesRequestDTO emailTemplatesRequestDTO)
    {
        return Ok(new ApiResponse<object>
        {
            Result = true,
            Message = await emailTemplatesService.AddOrEditEmailTemplate(emailTemplatesRequestDTO),
            StatusCode = 200,
            Data = null
        });
    }

    [HttpGet("get-email-template-by-id/{id}")]
    public async Task<IActionResult> GetEmailTemplateById(int id)
    {
        return Ok(new ApiResponse<EmailTemplatesResponseDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await emailTemplatesService.GetEmailTemplateById(id)
        });
    }

    [HttpPut("update-email-template-by-action")]
    public async Task<IActionResult> UpdateEmailTemplateByAction([FromBody] EmailTemplateActionRequestDTO emailTemplateActionRequest)
    {
        return Ok(new ApiResponse<object>
        {
            Result = true,
            Message = await emailTemplatesService.UpdateEmailTemplateByAction(emailTemplateActionRequest),
            StatusCode = 200,
            Data = null
        });
    }
}