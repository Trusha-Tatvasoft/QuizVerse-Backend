using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRoles.Player))]
[Route("api/[controller]")]
public class UserBattlesController(IUserBattlesService userBattlesService) : ControllerBase
{
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
}
