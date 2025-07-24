using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LandingPageController(ILandingPageService landingPageService) : ControllerBase
{
    [HttpGet("GetLandingPageData")]
    public async Task<IActionResult> GetLandingPageData()
    {
        ApiResponse<LandingPageData> response = new ApiResponse<LandingPageData>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Landing page data retrieved successfully",
            Data = await landingPageService.GetLandingPageDataAsync()
        };

        return Ok(response);
    }
}
