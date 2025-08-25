using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.WebAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = nameof(UserRoles.Player))]
    [Route("api/[controller]")]
    public class LeaderboardController(ILeaderboardService _leaderboardService) : ControllerBase
    {
        [HttpGet("get-user-leaderboard-stats")]
        public async Task<IActionResult> GetUserLeaderboardStats()
        {
            return Ok(new ApiResponse<UserPerformanceResponseDto>
            {
                Result = true,
                Message = Constants.USER_LEADERBOARD_STATS_RETRIEVED,
                StatusCode = StatusCodes.Status200OK,
                Data = await _leaderboardService.GetUserLeaderboardStats()
            });
        }

        [HttpGet("get-leaderboard-gloabal-ranking")]
        public async Task<IActionResult> GetLeaderboardGlobalRanking()
        {
            return Ok(new ApiResponse<List<LeaderboardGlobalRankingResponseDto>>
            {
                Result = true,
                Message = Constants.GLOBAL_LEADERBOARD_RETRIEVED_SUCCESSFULLY,
                StatusCode = StatusCodes.Status200OK,
                Data = await _leaderboardService.GetLeaderboardGlobalRanking()
            });
        }

    }
}
