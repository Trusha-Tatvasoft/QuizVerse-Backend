using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    #region Get by Id
    // GET: api/users/details/{id}
    [HttpGet("details/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(new ApiResponse<UserDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await userService.GetUserById(id)
        });
    }
    #endregion

    #region Create or Update
    // POST: api/users/save
    [HttpPost("save")]
    public async Task<IActionResult> CreateOrUpdate([FromBody] UserRequestDto dto)
    {
        var (success, message) = await userService.CreateOrUpdateUser(dto);

        bool isUpdate = dto.Id.HasValue && dto.Id > 0;

        return Ok(new ApiResponse<object>
        {
            Result = success,
            Message = message,
            StatusCode = isUpdate ? 200 : 201,
            Data = null
        });
    }
    #endregion


    #region Update by Action
    // PUT: api/users/action
    [HttpPut("action")]
    public async Task<IActionResult> UserAction([FromBody] UserActionRequest userActionRequest)
    {
        return Ok(new ApiResponse<string>
        {
            Result = true,
            Message = await userService.UpdateUserByAction(userActionRequest),
            StatusCode = 200,
            Data = null
        });
    }
    #endregion
}