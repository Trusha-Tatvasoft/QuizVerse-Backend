using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("landing-page")]
public class LandingPageController : ControllerBase
{
    private readonly ILandingPageService _landingPageService;

    public LandingPageController(ILandingPageService landingPageService)
    {
        _landingPageService = landingPageService;
    }

    [HttpGet(Name = "GetLandingPageData")]
    public async Task<IActionResult> GetLandingPageData()
    {
        ApiResponse<LandingPageData> response = new ApiResponse<LandingPageData>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Landing page data retrieved successfully",
            Data = await _landingPageService.GetLandingPageDataAsync()
        };

        return Ok(response);
    }
}
