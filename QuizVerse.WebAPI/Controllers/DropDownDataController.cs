using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DropDownDataController(IDropDownDataService _dropDownService) : ControllerBase
{
    [HttpGet("get-dropdown-data")]
    public IActionResult GetDropDownData([FromQuery] DropDownType type)
    {
        ApiResponse<List<CommonListDropDownDto>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = $"{type} " + Constants.FETCH_SUCCESS,
            Data = _dropDownService.GetDropDownListData(type)
        };

        return Ok(response);
    }
}