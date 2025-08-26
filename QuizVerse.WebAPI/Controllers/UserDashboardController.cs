using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserDashboardController(IUserDashboardService _userDashboardService) : ControllerBase
{
    [HttpGet("get-statistics-data")]
    public async Task<IActionResult> GetStatisticsData()
    {
        ApiResponse<UserDashboardResponse> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.USER_DASHBOARD_SUMMARY_FETCH,
            Data = await _userDashboardService.GetStatisticsData()
        };

        return Ok(response);
    }

    [HttpGet("get-recent-quizzes")]
    public async Task<IActionResult> GetRecentQuizzes([FromQuery] bool ViewAll = false)
    {
        ApiResponse<List<RecentQuizResponse>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.RECENT_QUIZZES_FETCHED,
            Data = await _userDashboardService.GetRecentQuizzes(ViewAll)
        };

        return Ok(response);
    }

    [HttpGet("get-featured-quizzes")]
    public async Task<IActionResult> GetFeaturedQuizzes([FromQuery] int BatchNumber = 1)
    {
        ApiResponse<FeaturedQuizListDTO> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.FEATURED_QUIZZES_FETCHED,
            Data = await _userDashboardService.GetFeaturedQuizzes(BatchNumber)
        };

        return Ok(response);
    }

    [HttpGet("get-battle-requests")]
    public async Task<IActionResult> GetBattleRequests()
    {
        ApiResponse<List<BattleRequestDTO>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.BATTLE_REQUESTS_FETCHED,
            Data = await _userDashboardService.GetBattleRequests()
        };

        return Ok(response);
    }

    [HttpPut("update-battle-request-status")]
    public async Task<IActionResult> UpdateBattleRequestStatus([FromBody] BattleRequestActionDTO dto)
    {
        ApiResponse<bool> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.BATTLE_STATUS_UPDATED,
            Data = await _userDashboardService.UpdateBattleRequestStatus(dto)
        };

        return Ok(response);
    }

    [HttpGet("get-rank-progress")]
    public async Task<IActionResult> GetRankProgress()
    {
        ApiResponse<RankProgressDTO> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.RANK_PROGRESS_FETCHED,
            Data = await _userDashboardService.GetRankProgressAsync()
        };

        return Ok(response);
    }
}
