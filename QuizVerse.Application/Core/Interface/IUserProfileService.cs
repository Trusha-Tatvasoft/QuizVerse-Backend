using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IUserProfileService
{
    Task<UserBasicProfileDto> GetUserBasicProfile();
    Task<UserOverviewDto> GetUserOverview();
    Task<UserNavbarDataDto> GetUserNavbarData();
    Task<bool> UpdateProfilePicture(UpdateProfilePicRequestDto updateProfilePicRequestDto);
    Task<List<UserBadgesResponseDto>> GetUserBadges();
    Task<UserProfileSettingDto> GetUserProfileSetting();
    Task<CreateUpdateResponseDto> UpdateUserProfile(UserProfileSettingDto userProfileSettingDto);
    Task<bool> IsEmailAvailable(string email);
    Task<string> SendOtpToUser(UserProfileSettingDto userProfileSettingDto);
    Task<bool> VerifyOtp(VerifyOtpRequestDto request);
    Task<AdminProfileResponseDto> GetAdminProfile();
    Task<CreateUpdateResponseDto> UpdateAdminProfile(AdminProfileRequestDto UpdatedAdminProfile);
}
