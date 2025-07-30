using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdminDashboardController(IAdminDashboardService _adminDashboardService) : ControllerBase
{
    [HttpGet("get-statistics-data")]
    public async Task<IActionResult> GetStatisticsData()
    {
        ApiResponse<AdminDashboardResponse> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.DASHBOARD_SUMMARY_FETCH,
            Data = await _adminDashboardService.GetStatisticsData()
        };

        return Ok(response);
    }

    [HttpGet("get-user-engagement-data")]
    public async Task<IActionResult> GetUserEngagementData(string start_date, string end_date)
    {
        BadRequestObjectResult? validationResult = ValidateDateRange(start_date, end_date);
        if (validationResult != null) return validationResult;

        ApiResponse<List<ChartDataDTO>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.USER_ENGAGEMENT_DATA_FETCH,
            Data = await _adminDashboardService.GetUserEngagementData(start_date, end_date)
        };

        return Ok(response);
    }

    [HttpGet("get-revenue-trend-data")]
    public async Task<IActionResult> GetRevenueTrendData(string start_date, string end_date)
    {
        BadRequestObjectResult? validationResult = ValidateDateRange(start_date, end_date);
        if (validationResult != null) return validationResult;

        ApiResponse<List<ChartDataDTO>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.REVENUE_TREND_DATA_FETCH,
            Data = await _adminDashboardService.GetRevenueTrendData(start_date, end_date)
        };

        return Ok(response);
    }

    [HttpGet("get-performance-score-data")]
    public async Task<IActionResult> GetPerformanceScoreData(string start_date, string end_date)
    {
        BadRequestObjectResult? validationResult = ValidateDateRange(start_date, end_date);
        if (validationResult != null) return validationResult;

        ApiResponse<List<ChartDataDTO>> response = new()
        {
            Result = true,
            StatusCode = StatusCodes.Status200OK,
            Message = Constants.PERFORMANCE_SCORE_DATA_FETCH,
            Data = await _adminDashboardService.GetPerformanceScoreData(start_date, end_date)
        };

        return Ok(response);
    }

    private BadRequestObjectResult? ValidateDateRange(string start_date, string end_date)
    {
        if (string.IsNullOrWhiteSpace(start_date))
        {
            return BadRequest(new ApiResponse<string>
            {
                Result = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = Constants.START_DATE_REQUIRED,
                Data = null
            });
        }

        if (string.IsNullOrWhiteSpace(end_date))
        {
            return BadRequest(new ApiResponse<string>
            {
                Result = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = Constants.END_DATE_REQUIRED,
                Data = null
            });
        }

        if (!DateTime.TryParse(start_date, out DateTime startDate))
        {
            return BadRequest(new ApiResponse<string>
            {
                Result = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = Constants.INVALID_START_DATE_FORMAT,
                Data = null
            });
        }

        if (!DateTime.TryParse(end_date, out DateTime endDate))
        {
            return BadRequest(new ApiResponse<string>
            {
                Result = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = Constants.INVALID_END_DATE_FORMAT,
                Data = null
            });
        }

        if (startDate > endDate)
        {
            return BadRequest(new ApiResponse<string>
            {
                Result = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = Constants.START_DATE_AFTER_END_DATE,
                Data = null
            });
        }

        return null;
    }
}
