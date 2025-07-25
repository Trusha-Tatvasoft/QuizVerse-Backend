using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers;
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    #region GetById
    // GET: api/users/details/{id}
    [HttpGet("details/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserById(id);
        return Ok(new ApiResponse<UserDto>
        {
            Result = true,
            Message = Constants.Messages.FETCH_SUCCESS,
            StatusCode = 200,
            Data = user
        });
    }
    #endregion

    #region Create
    // POST: api/users/create
    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        var createdUser = await _userService.CreateUser(dto);
        return StatusCode(201, new ApiResponse<UserDto>
        {
            Result = true,
            Message = Constants.Messages.CREATE_SUCCESS,
            StatusCode = 201,
            Data = createdUser
        });
    }
    #endregion

    #region Update
    // PUT: api/users/edit/{id}
    [HttpPut("edit/{id}")]
    public async Task<IActionResult> Update(int id, UpdateUserDto dto)
    {
        var updatedUser = await _userService.UpdateUser(id, dto);
        return Ok(new ApiResponse<UserDto>
        {
            Result = true,
            Message = Constants.Messages.UPDATE_SUCCESS,
            StatusCode = 200,
            Data = updatedUser
        });
    }
    #endregion

    #region Delete
    // DELETE: api/users/delete/{id}
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteUser(id);
        return Ok(new ApiResponse<string>
        {
            Result = true,
            Message = Constants.Messages.DELETE_SUCCESS,
            StatusCode = 200,
            Data = null
        });
    }
    #endregion

    #region Change Status
    // PATCH: api/users/change-status/{id}?status=Active
    [HttpPatch("change-status/{id}")]
    public async Task<IActionResult> ChangeStatus(int id, [FromQuery] UserStatus? status)
    {
        if (status == null)
            throw new AppException(string.Format(Constants.Messages.STATUS_REQUIRED));

        await _userService.ChangeUserStatus(id, status.Value);

        return Ok(new ApiResponse<string>
        {
            Result = true,
            Message = string.Format(Constants.Messages.STATUS_CHANGED, status),
            StatusCode = 200,
            Data = null
        });
    }
    #endregion

}
