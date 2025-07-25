using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LandingPageController(ILandingPageService landingPageService) : ControllerBase
{
    [HttpGet("get-landing-page-data")]
    public async Task<IActionResult> GetLandingPageData()
    {
        ApiResponse<LandingPageData> response = new ApiResponse<LandingPageData>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.Messages.FETCH_SUCCESS,
            Data = await landingPageService.GetLandingPageData()
        };

        return Ok(response);
    }
}
