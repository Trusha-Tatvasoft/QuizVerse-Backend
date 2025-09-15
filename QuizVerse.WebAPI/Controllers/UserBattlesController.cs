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

    #region User Recent Battles
    [HttpGet("get-user-recent-battles")]
    public async Task<IActionResult> GetUserRecentBattles()
    {
        return Ok(new ApiResponse<List<UserRecentBattleDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userBattlesService.GetUserRecentBattles()
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

    #endregion
}
