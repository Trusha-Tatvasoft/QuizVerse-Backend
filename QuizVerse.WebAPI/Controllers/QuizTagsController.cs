using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizTagsController(IQuizTagsService _quizTagsService) : ControllerBase
{
    [HttpGet("get-all-quiz-tags")]
    public IActionResult GetAllQuizTags()
    {
        ApiResponse<List<CommonListDropDownDto>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = _quizTagsService.GetAllQuizTags()
        };

        return Ok(response);
    }
}
