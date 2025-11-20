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
[Authorize(Roles = Constants.RoleGroups.Admins)]
[ApiController]
public class QuestionPoolController(IQuestionPoolService _questionPoolService, IAiQuestionGenerationService _questionFromText) : ControllerBase
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

    [HttpPost("save-questions")]
    public async Task<IActionResult> SaveQuestions([FromBody] List<QuestionsListRequestDto> questionList)
    {
        ApiResponse<PageListResponse<QuestionPoolListDto>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = await _questionPoolService.SaveQuestions(questionList),
            Data = null
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

    [HttpPost("generate-from-text-prompt")]
    [Consumes("application/json")]
    public async Task<IActionResult> GenerateFromTextPrompt([FromBody] GenerateQuizRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new ApiResponse<object>
            {
                Result = false,
                Message = Constants.PROVIDE_PROPER_TEXT_PROMPT,
                StatusCode = 400
            });
        }

        var result = await _questionFromText.GenerateFromPromptAsync(request);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new ApiResponse<object>
            {
                Result = false,
                Message = result.Message,
                StatusCode = result.StatusCode,
                Data = null
            });
        }

        return Ok(new ApiResponse<List<QuizQuestionDto>>
        {
            Result = true,
            Message = result.Message,
            StatusCode = 200,
            Data = result.Data
        });
    }

    [HttpPost("generate-question-using-web-url")]
    [Consumes("application/json")]
    public async Task<IActionResult> GenerateQuestionUsingWebURL([FromBody] GenerateQuestionUsingWebRequestDTO request)
    {
        var result = await _questionFromText.GenerateQuestionUsingWebURL(request);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new ApiResponse<object>
            {
                Result = false,
                Message = result.Message,
                StatusCode = result.StatusCode,
                Data = null
            });
        }

        return Ok(new ApiResponse<List<QuizQuestionDto>>
        {
            Result = true,
            Message = result.Message,
            StatusCode = 200,
            Data = result.Data
        });
    }

    [HttpPost("generate-from-pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> GenerateFromPdf([FromForm] GenerateQuizFromPDFRequest request)
    {
        var result = await _questionFromText.GenerateFromPdfAsync(request);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new ApiResponse<object>
            {
                Result = false,
                Message = result.Message,
                StatusCode = result.StatusCode,
                Data = null
            });
        }

        return Ok(new ApiResponse<List<QuizQuestionDto>>
        {
            Result = true,
            Message = result.Message,
            StatusCode = 200,
            Data = result.Data
        });
    }

}