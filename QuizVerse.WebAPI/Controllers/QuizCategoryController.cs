using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizCategoryController(IQuizCategoryService _quizCategoryService) : ControllerBase
{
    [HttpPost("get-quiz-categories")]
    public async Task<IActionResult> GetQuizCategories(PageListRequest pageListRequest)
    {
        ApiResponse<PageListResponse<QuizCategoryDTO>> response = new ApiResponse<PageListResponse<QuizCategoryDTO>>
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = await _quizCategoryService.GetQuizCategories(pageListRequest)
        };
        return Ok(response);
    }
}
