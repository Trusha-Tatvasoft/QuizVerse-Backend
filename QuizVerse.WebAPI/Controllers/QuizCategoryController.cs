using System.Reflection.Metadata;
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
        string resultMessage = await _quizCategoryService.UpdateQuizCategoryByAction(quizCategoryAction);

        return Ok(new ApiResponse<object>
        {
            Result = true,
            Message = resultMessage,
            StatusCode = 200,
            Data = null
        });
    }
    #endregion
}
