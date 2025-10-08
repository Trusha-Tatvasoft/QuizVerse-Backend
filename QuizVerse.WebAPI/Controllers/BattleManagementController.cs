using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = Constants.RoleGroups.Admins)]
[Route("api/[controller]")]
public class BattleManagementController(IBattleManagementService battleManagementService) : ControllerBase
{
    #region Battle List Data 
    [HttpGet("get-battle-list")]
    public async Task<IActionResult> GetBattleList()
    {
        return Ok(new ApiResponse<List<BattleManagementData>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await battleManagementService.GetBattleList()
        });
    }
    #endregion

    #region Create/Update Battle
    [HttpPost("create-update-battle")]
    public async Task<IActionResult> CreateUpdateBattle([FromBody] SaveBattleRequestDTO battleCreateUpdateRequestDto)
    {
        CreateUpdateResponseDto response = await battleManagementService.CreateUpdateBattle(battleCreateUpdateRequestDto);
        return Ok(new ApiResponse<CreateUpdateResponseDto>
        {
            Result = response.Success,
            Message = response.Message,
            StatusCode = StatusCodes.Status200OK,
            Data = null
        });
    }
    #endregion

    #region Get Battle By Id
    [HttpGet("get-battle-by-id/{battleId}")]
    public async Task<IActionResult> GetBattleById(int battleId)
    {
        return Ok(new ApiResponse<BattleResponseDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await battleManagementService.GetBattleById(battleId)
        });
    }
    #endregion

    #region Delete Battle
    [HttpDelete("delete-Battle/{BattleId}")]
    public async Task<IActionResult> DeleteBattle(int BattleId)
    {
        return Ok(new ApiResponse<CreateUpdateResponseDto>
        {
            Result = true,
            Message = await battleManagementService.DeleteBattle(BattleId),
            StatusCode = StatusCodes.Status200OK,
            Data = null
        });
    }
    #endregion
}
