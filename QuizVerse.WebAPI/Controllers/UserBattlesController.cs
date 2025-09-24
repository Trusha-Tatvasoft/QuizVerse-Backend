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
[Authorize(Roles = nameof(UserRoles.Player))]
[Route("api/[controller]")]
public class UserBattlesController(IUserBattlesService userBattlesService) : ControllerBase
{
    #region User Available Battles
    [HttpGet("get-user-available-battles")]
    public async Task<IActionResult> GetUserAvailableBattles()
    {
        return Ok(new ApiResponse<List<UserAvailableBattleDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userBattlesService.GetUserAvailableBattles()
        });
    }
    #endregion

    #region User Battles History
    [HttpPost("get-user-battle-history")]
    public async Task<IActionResult> GetUserBattleHistory([FromBody] UserBattleHistoryRequestDto request)
    {
        return Ok(new ApiResponse<UserBattleHistoryResponseDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userBattlesService.GetUserBattleHistory(request)
        });
    }
    #endregion

    #region Battle Leaderboard Data 
    [HttpGet("get-battle-leaderboard-list")]
    public async Task<IActionResult> GetBattleLeaderboardList()
    {
        return Ok(new ApiResponse<List<UserBattleLeaderboardData>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = 200,
            Data = await userBattlesService.GetBattleLeaderboardList()
        });
    }
    #endregion

    #region Battle Request Management
    [HttpPost("send-battle-request")]
    public async Task<IActionResult> SendBattleRequest([FromBody] SendBattleRequestDTO dto)
    {
        return Ok(new ApiResponse<string>
        {
            Result = true,
            Message = await userBattlesService.SendBattleRequest(dto),
            StatusCode = StatusCodes.Status200OK,
            Data = null
        });
    }

    [HttpGet("check-user-existence/{userName}")]
    public async Task<IActionResult> CheckUserExistence(string userName)
    {
        return Ok(new ApiResponse<bool>
        {
            Result = true,
            Message = Constants.USERNAME_AVAILABILITY_VERIFIED,
            StatusCode = StatusCodes.Status200OK,
            Data = await userBattlesService.CheckUserExistence(userName)
        });
    }
    #endregion

    #region Get Battle Result
    [HttpGet("get-battle-result/{battleId}")]
    public async Task<IActionResult> GetBattleResult(int battleId)
    {
        return Ok(new ApiResponse<UserBattleResult>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userBattlesService.GetBattleResult(battleId),
        });
    }
    #endregion
}
