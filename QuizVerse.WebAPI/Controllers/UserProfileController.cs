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
public class UserProfileController(IUserProfileService userProfileService) : ControllerBase
{
    #region Get User Profile
    [HttpGet("get-user-basic-profile")]
    public async Task<IActionResult> GetUserBasicProfile()
    {
        return Ok(new ApiResponse<UserBasicProfileDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userProfileService.GetUserBasicProfile()
        });
    }

    [HttpGet("get-user-overview")]
    public async Task<IActionResult> GetUserOverview()
    {

        return Ok(new ApiResponse<UserOverviewDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userProfileService.GetUserOverview()
        });
    }
    #endregion

    #region Update ProfilePic
    [HttpPost("update-profile-pic")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProfilePic([FromForm] UpdateProfilePicRequestDto dto)
    {
        return Ok(new ApiResponse<string>
        {
            Result = await userProfileService.UpdateProfilePicture(dto),
            Message = Constants.UPDATE_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = null
        });
    }
    #endregion

    #region Get UserBadges
    [HttpGet("get-user-badges")]
    public async Task<IActionResult> GetUserBadges()
    {
        return Ok(new ApiResponse<List<UserBadgesResponseDto>>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userProfileService.GetUserBadges()
        });
    }
    #endregion

    #region  User Profile update
    [HttpGet("get-user-profile-setting")]
    public async Task<IActionResult> GetUserProfileSetting()
    {
        return Ok(new ApiResponse<UserProfileSettingDto>
        {
            Result = true,
            Message = Constants.FETCH_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userProfileService.GetUserProfileSetting()
        });
    }

    [HttpPut("update-user-profile")]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileSettingDto request)
    {
        var result = await userProfileService.UpdateUserProfile(request);

        return Ok(new ApiResponse<string>
        {
            Result = result.Success,
            Message = result.Message,
            StatusCode = StatusCodes.Status200OK,
            Data = null
        });
    }

    [HttpGet("is-email-available")]
    public async Task<IActionResult> IsEmailAvailable(string email)
    {
        return Ok(new ApiResponse<bool>
        {
            Result = true,
            Message = Constants.VALID_DATA,
            StatusCode = StatusCodes.Status200OK,
            Data = await userProfileService.IsEmailAvailable(email)
        });
    }

    [HttpPost("send-otp-to-user")]
    public async Task<IActionResult> SendOtpToUser([FromBody] UserProfileSettingDto userProfileSettingDto)
    {
        return Ok(new ApiResponse<string>
        {
            Result = true,
            Message = Constants.SENT_SUCCESS,
            StatusCode = StatusCodes.Status200OK,
            Data = await userProfileService.SendOtpToUser(userProfileSettingDto)
        });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        return Ok(new ApiResponse<bool>
        {
            Result = true,
            Message = Constants.VALID_DATA,
            StatusCode = StatusCodes.Status200OK,
            Data = await userProfileService.VerifyOtp(request)
        });
    }
    #endregion
}