using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRoles.Admin))]
[ApiController]
public class QuestionPoolController(IQuestionPoolService _questionPoolService) : ControllerBase
{
    [HttpPost("create-or-update-question/{id:int}")]
    public async Task<IActionResult> CreateOrUpdateQuestion(int id, [FromBody] QuestionRequestDTO dto)
    {
        if (id < 0)
            throw new AppException(Constants.INVALID_QUESTION_ID_MESSAGE, StatusCodes.Status400BadRequest);

        ApiResponse<object> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = await _questionPoolService.CreateOrUpdateQuestion(id, dto),
            Data = null
        };

        return Ok(response);
    }

    [HttpDelete("delete-question/{id:int}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        if (id <= 0)
            throw new AppException(Constants.INVALID_QUESTION_ID_MESSAGE, StatusCodes.Status400BadRequest);

        ApiResponse<object> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = await _questionPoolService.DeleteQuestion(id),
            Data = null
        };

        return Ok(response);
    }

    [HttpGet("get-question-preview/{id:int}")]
    public async Task<IActionResult> GetQuestionPreview(int id)
    {
        if (id <= 0)
            throw new AppException(Constants.INVALID_QUESTION_ID_MESSAGE, StatusCodes.Status400BadRequest);

        ApiResponse<QuestionDetailDTO> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.QUESTION_PREVIEW_FETCH_SUCCESS_MESSAGE,
            Data = await _questionPoolService.GetQuestionPreview(id),
        };

        return Ok(response);
    }

    [HttpPost("get-question-pool-list")]
    public async Task<IActionResult> GetQuestionPoolListAsync([FromBody] PageListRequest pageListRequest)
    {
        ApiResponse<PageListResponse<QuestionPoolListDto>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FETCH_SUCCESS,
            Data = await _questionPoolService.GetQuestionPoolListAsync(pageListRequest)
        };

        return Ok(response);
    }

    [HttpPost("import-questions-from-csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportQuestionsFromCsv([FromForm] CsvUploadRequestDTO request)
    {
        IFormFile file = request.File;

        using Stream stream = file.OpenReadStream();

        ApiResponse<object> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = await _questionPoolService.ImportQuestionsFromCsv(stream),
            Data = null,
        };

        return Ok(response);
    }

    [HttpPost("import-questions-from-excel")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportQuestionsFromExcel([FromForm] ExcelUploadRequestDTO request)
    {
        IFormFile file = request.File;

        using Stream stream = file.OpenReadStream();

        ApiResponse<object> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = await _questionPoolService.ImportQuestionsFromExcel(stream),
            Data = null,
        };

        return Ok(response);
    }

    [HttpPost("preview-questions-from-csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PreviewQuestionsFromCsv([FromForm] CsvUploadRequestDTO request)
    {
        IFormFile file = request.File;

        using Stream stream = file.OpenReadStream();

        ApiResponse<object> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.CSV_PREVIEW_LOADED_SUCCESSFULLY,
            Data = await _questionPoolService.PreviewQuestionsFromCsv(stream)
        };

        return Ok(response);
    }

    [HttpPost("preview-questions-from-excel")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PreviewQuestionsFromExcel([FromForm] ExcelUploadRequestDTO request)
    {
        IFormFile file = request.File;

        using Stream stream = file.OpenReadStream();

        ApiResponse<object> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.EXCEL_PREVIEW_LOADED_SUCCESSFULLY,
            Data = await _questionPoolService.PreviewQuestionsFromExcel(stream)
        };

        return Ok(response);
    }
}