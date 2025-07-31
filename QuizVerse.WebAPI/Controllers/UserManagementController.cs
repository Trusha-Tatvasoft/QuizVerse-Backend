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
// [Authorize(Roles = nameof(UserRoles.Admin))]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    #region List Users 
    [HttpPost("get-users-by-pagination")]
    public async Task<IActionResult> GetUsersByPagination([FromBody] PageListRequest query)
    {
        return Ok(new ApiResponse<PageListResponse<UserDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await userService.GetUsersByPagination(query)
        });
    }
    #endregion

    #region Get by Id
    // GET: api/users/get-user-by-id/{id}
    [HttpGet("get-user-by-id/{id}")]
    public async Task<IActionResult> GetUserById(int id)
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
    // POST: api/users/create-or-update-user
    [HttpPost("create-or-update-user")]
    public async Task<IActionResult> CreateOrUpdateUser([FromBody] UserRequestDto dto)
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
    // PUT: api/users/update-user-status-by-action
    [HttpPut("update-user-status-by-action")]
    public async Task<IActionResult> UpdateUserByAction([FromBody] UserActionRequest userActionRequest)
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

    #region Export Data
    // POST: api/users/user-export-data
    [HttpPost("user-export-data")]
    public async Task<IActionResult> UserExportData([FromBody] PageListRequest query)
    {
        MemoryStream fileStream = await userService.UserExportData(query);
        string fileName = $"Users_{DateTime.UtcNow:dd-MM-yyyy}.xlsx";

        return File(
            fileStream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName
        );
    }
    #endregion
}