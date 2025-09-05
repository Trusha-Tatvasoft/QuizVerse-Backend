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

        [HttpGet("get-weekly-leaderboard-ranking")]
        public async Task<IActionResult> GetWeeklyLeaderboardRanking()
        {
            return Ok(new ApiResponse<List<WeeklyLeaderBoardResponseDto>>
            {
                Result = true,
                Message = Constants.WEEKLY_LEADERBOARD_RETRIEVED,
                StatusCode = StatusCodes.Status200OK,
                Data = await _leaderboardService.GetWeeklyLeaderboardRanking()
            });
        }

        [HttpGet("get-category-wise-leaderboard")]
        public async Task<IActionResult> GetCategoryWiseLeaderboard(int categoryId)
        {
            return Ok(new ApiResponse<List<CategoryWiseLeaderBoardResponseDto>>
            {
                Result = true,
                Message = Constants.CATEGORY_WISE_LEADERBOARD_RETRIEVED,
                StatusCode = StatusCodes.Status200OK,
                Data = await _leaderboardService.GetQuizCategoryWiseLeaderboardRanking(categoryId)
            });
        }

        [HttpGet("get-monthly-champions")]
        public async Task<IActionResult> GetMonthlyChampions(int month, int year)
        {
            return Ok(new ApiResponse<List<MonthlyChampionsResponseDto>>
            {
                Result = true,
                Message = Constants.MONTHLY_CHAMPIONS_RETRIEVED,
                StatusCode = StatusCodes.Status200OK,
                Data = await _leaderboardService.GetMonthlyChampions(month, year)
            });
        }
    }
}
