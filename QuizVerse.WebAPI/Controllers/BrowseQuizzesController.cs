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
[Authorize(Roles = nameof(UserRoles.Player))]
public class BrowseQuizzesController(IBrowseQuizzesService browseQuizzesService): ControllerBase
{
    [HttpPost("browse-quizzes")]
    public async Task<IActionResult> BrowseQuizzes(BrowseQuizzesRequestDTO request)
    {
        ApiResponse<BrowseQuizzesResponseDTO> response = new ApiResponse<BrowseQuizzesResponseDTO>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = await browseQuizzesService.BrowseQuizzes(request),
        };
        return Ok(response);
    }
}
