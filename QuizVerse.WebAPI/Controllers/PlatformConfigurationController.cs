using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlatformConfigurationController(IPlatformConfigurationService platformConfigurationService) : ControllerBase
{
    [HttpGet("get-platform-configurations")]
    public async Task<IActionResult> GetPlatformConfigurations()
    {
        ApiResponse<PlateformConfigurationResponseDTO> response = new ApiResponse<PlateformConfigurationResponseDTO>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = await platformConfigurationService.GetPlatformConfigurations()
        };
        return Ok(response);
    }

    [HttpPost("update-platform-configurations")]
    public async Task<IActionResult> UpdatePlatformConfigurations([FromForm]PlatformConfigurationRequestDTO platformConfigurationRequest)
    {
        ApiResponse<object> response = new ApiResponse<object>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = await platformConfigurationService.UpdatePlatformConfigurations(platformConfigurationRequest),
            Data = null
        };
        return Ok(response);
    }
}
