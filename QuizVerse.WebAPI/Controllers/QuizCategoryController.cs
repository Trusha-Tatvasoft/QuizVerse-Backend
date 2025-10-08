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
[Authorize(Roles = nameof(Constants.RoleGroups.Admins))]
[Route("api/[controller]")]
public class QuizCategoryController(IQuizCategoryService _quizCategoryService) : ControllerBase
{
    [HttpPost("get-quiz-categories")]
    public async Task<IActionResult> GetQuizCategories(PageListRequest pageListRequest)
    {
        ApiResponse<PageListResponse<QuizCategoryDTO>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = await _quizCategoryService.GetQuizCategories(pageListRequest)
        };
        return Ok(response);
    }


    #region Get quiz category by Id

    [HttpGet("get-quiz-category-by-id/{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        return Ok(new ApiResponse<QuizCategoryDTO>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await _quizCategoryService.GetQuizCategoryById(id)
        });
    }
    #endregion

    #region  Create or Update category
    [HttpPost("create-or-update-quiz-category")]
    public async Task<IActionResult> CreateOrUpdateQuizCategory([FromBody] QuizCategoryDTO quizCategoryDTO)
    {
        (bool success, string message) = await _quizCategoryService.CreateOrUpdateQuizCategory(quizCategoryDTO);

        bool isUpdate = quizCategoryDTO.Id.HasValue && quizCategoryDTO.Id > 0;

        return Ok(new ApiResponse<object>
        {
            Result = success,
            Message = message,
            StatusCode = isUpdate ? 200 : 201,
            Data = null
        });
    }
    #endregion

    #region  update quiz category by action
    [HttpPut("update-quiz-category-by-action")]
    public async Task<IActionResult> UpdateQuizCategoryByAction([FromBody] QuizCategoryActionRequestDto quizCategoryAction)
    {
        return Ok(new ApiResponse<object>
        {
            Result = true,
            Message = await _quizCategoryService.UpdateQuizCategoryByAction(quizCategoryAction),
            StatusCode = 200,
            Data = null
        });
    }
    #endregion

    #region Category Name Available
    [HttpGet("is-category-name-available")]
    public async Task<IActionResult> IsCategoryNameAvailable([FromQuery] string categoryName, [FromQuery] int? id = null)
    {
        return Ok(new ApiResponse<string>
        {
            Result = await _quizCategoryService.IsCategoryNameAvailable(categoryName, id),
            Message = Constants.VALID_DATA,
            StatusCode = StatusCodes.Status200OK,
            Data = null
        });
    }
    #endregion
}
