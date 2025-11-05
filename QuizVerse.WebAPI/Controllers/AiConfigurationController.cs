using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = Constants.RoleGroups.Admins)]
[Route("api/[controller]")]
public class AiConfigurationController(IAiConfigurationService _aiConfigurationService) : ControllerBase
{
    [HttpGet("get-ai-configuration-card-details")]
    public async Task<IActionResult> GetAiConfigurationCardDetails()
    {
        return Ok(new ApiResponse<AiConfigurationCardDetailsDTO>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await _aiConfigurationService.GetAiConfigurationCardDetails()
        });
    }

    [HttpGet("get-ai-uses-details")]
    public async Task<IActionResult> GetAiUsesDetails(AiModelName? aiModelName)
    {
        return Ok(new ApiResponse<AiUsesDetailsDTO>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await _aiConfigurationService.GetAiUsesDetails(aiModelName)
        });
    }
}