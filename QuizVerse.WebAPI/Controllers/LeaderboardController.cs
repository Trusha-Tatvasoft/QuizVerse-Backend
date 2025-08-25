using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController(ILeaderboardService _leaderboardService) : ControllerBase
    {
        [HttpGet("get-user-leaderboard-stats")]
        public async Task<IActionResult> GetUserLeaderboardStats()
        {
            return Ok(new
            {
                Success = true,
                Data =  await _leaderboardService.GetUserLeaderboardStats(),
                message = Constants.FETCH_DATA_MESSAGE,
                
            });
        }

        [HttpGet("get-leaderboard-gloabal-ranking")]
        public async Task<IActionResult> GetLeaderboardGlobalRanking()
        {
            return Ok(new ApiResponse<List<LeaderboardGlobalRankingResponseDto>>
            {
                Result = true,
                Message = "Global leaderboard fetched successfully",
                StatusCode = StatusCodes.Status200OK,
                Data = await _leaderboardService.GetLeaderboardGlobalRanking()
            });
        }

    }
}
