using AutoMapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class UserProfileService(
    ISqlQueryRepository sqlQueryRepository,
    IGenericRepository<User> userRepository,
    ICommonService commonService,
    IGenericRepository<Badge> badgeRepository,
    IGenericRepository<UserBadgesEarned> userBadgeRepository,
    IMapper mapper,
    IHttpContextAccessor httpContextAccessor) : IUserProfileService
{
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
    public string UserRole => httpContextAccessor.HttpContext?.User?.GetUserRole() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);


    #region GetUserProfile Data
    public async Task<UserBasicProfileDto> GetUserBasicProfile()
    {
        string query = string.Format(SqlConstants.GET_USER_PROFILE_QUERY_TEMPLATE, SqlConstants.GET_USER_BASIC_PROFILE_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_user_id", NpgsqlDbType.Integer) { Value = UserId }
        };

        UserBasicProfileDto basicProfile = await sqlQueryRepository.SqlQuerySingleAsync<UserBasicProfileDto>(query, parameters)
         ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, UserId));

        return mapper.Map<UserBasicProfileDto>(basicProfile);
    }

    public async Task<UserOverviewDto> GetUserOverview()
    {
        string query = string.Format(SqlConstants.GET_USER_PROFILE_QUERY_TEMPLATE, SqlConstants.GET_USER_OVERVIEW_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_user_id", NpgsqlDbType.Integer) { Value = UserId }
        };

        return await sqlQueryRepository.SqlQuerySingleAsync<UserOverviewDto>(query, parameters)
           ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, UserId));

    }

    public async Task<UserNavbarDataDto> GetUserNavbarData()
    {
        string query = string.Format(
            SqlConstants.GET_USER_NAVBAR_QUERY_TEMPLATE,
            SqlConstants.GET_USER_NAVBAR_FUNCTION
        );

        var parameters = new NpgsqlParameter[]
        {
            new("p_user_id", NpgsqlDbType.Integer) { Value = UserId },
            new("p_admin_role_id", NpgsqlDbType.Integer) { Value = (int)UserRoles.Admin }
        };

        return await sqlQueryRepository.SqlQuerySingleAsync<UserNavbarDataDto>(query, parameters);
    }

    #endregion

    #region Get UserBadges
    public async Task<List<UserBadgesResponseDto>> GetUserBadges()
    {
        var allBadges = await badgeRepository.FindAsync(b => !b.IsDeleted);

        var earnedBadgeIds = (await userBadgeRepository.FindAsync(ub =>
                ub.UserId == UserId && !ub.IsDeleted))
            .Select(ub => ub.BadgeId)
            .ToHashSet();

        var result = mapper.Map<List<UserBadgesResponseDto>>(allBadges);

        foreach (var badge in result)
        {
            badge.Earned = earnedBadgeIds.Contains(badge.BadgeId);
        }

        result = [.. result
                .OrderByDescending(b => b.Earned)
                .ThenBy(b => b.BadgeId)];
        return result;
    }
    #endregion

    #region GetUserProfileSetting
    public async Task<UserProfileSettingDto> GetUserProfileSetting()
    {
        var user = await userRepository.GetAsync(u => u.Id == UserId && !u.IsDeleted)
                            ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, UserId));

        return mapper.Map<UserProfileSettingDto>(user);
    }
    #endregion

    #region UpdateProfile
    public async Task<CreateUpdateResponseDto> UpdateUserProfile(UserProfileSettingDto userProfileSettingDto)
    {
        if (string.IsNullOrWhiteSpace(userProfileSettingDto.FullName))
            throw new AppException(Constants.FULLNAME_REQUIRED);

        var trimDto = mapper.Map<UserProfileSettingDto>(userProfileSettingDto);

        bool isAvailable = await IsEmailAvailable(userProfileSettingDto.Email);
        if (!isAvailable)
            throw new AppException(Constants.EMAIL_ALREADY_IN_USE);

        string query = string.Format(
            SqlConstants.UPDATE_USER_PROFILE_QUERY_TEMPLATE,
            SqlConstants.UPDATE_USER_PROFILE_FUNCTION
        );

        var parameters = new NpgsqlParameter[]
        {
        new("p_current_user_id", NpgsqlDbType.Integer) { Value = UserId },
        new("p_new_email", NpgsqlDbType.Varchar) { Value = trimDto.Email },
        new("p_new_name", NpgsqlDbType.Varchar) { Value = trimDto.FullName },
        new("p_new_bio", NpgsqlDbType.Text) { Value = (object?)trimDto.Bio ?? DBNull.Value }
        };

        return await sqlQueryRepository.SqlQuerySingleAsync<CreateUpdateResponseDto>(query, parameters);
    }
    #endregion

    #region Update ProfilePic
    public async Task<bool> UpdateProfilePicture(UpdateProfilePicRequestDto updateProfilePicRequestDto)
    {
        var user = await userRepository.GetAsync(u => u.Id == UserId) ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, UserId));
        if(user.ProfilePic != null)
        {
            commonService.DeleteFile(user.ProfilePic);
        }

        var profilePicPath = await commonService.SaveFile(updateProfilePicRequestDto.ProfilePic, "users");

        user.ProfilePic = profilePicPath;
        user.ModifiedDate = DateTime.UtcNow;
        user.ModifiedBy = UserId;

        await userRepository.UpdateAsync(user);

        return true;
    }
    #endregion

    #region CheckEmailAvailable
    public async Task<bool> IsEmailAvailable(string email)
    {
        if (!Enum.TryParse<UserRoles>(UserRole, out var currentUserRole))
            throw new AppException(Constants.UNAUTHORIZED_USER);

        var normalizedEmail = email.ToLower().Trim();
        var user = await userRepository.GetAsync(u => u.Email.ToLower().Trim() == normalizedEmail);

        if (user == null)
            return true;

        // If the email belongs to the same user, it's available
        if (user.Id == UserId)
            return true;

        switch ((UserStatus)user.Status)
        {
            case UserStatus.Suspended:
                throw new AppException(Constants.EMAIL_SUSPENDED);

            case UserStatus.Active:
            case UserStatus.Inactive:
                if (user.IsDeleted)
                {
                    // strict separation: only allow if same role
                    if (user.RoleId == (int)currentUserRole)
                    {
                        return true;
                    }

                    // if roles differ → not allowed
                    throw new AppException(Constants.EMAIL_ALREADY_IN_USE_DIFFERENT_ROLE);
                }

                // active/inactive + not deleted → email is in use
                throw new AppException(Constants.EMAIL_ALREADY_IN_USE);

            default:
                throw new AppException(Constants.EMAIL_ALREADY_IN_USE);
        }
    }
    #endregion

    #region SendOtpToUser
    public async Task<string> SendOtpToUser(UserProfileSettingDto userProfileSettingDto)
    {
        var user = await userRepository.GetAsync(u => u.Id == UserId && !u.IsDeleted)
                   ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, UserId));

        bool isAvailable = await IsEmailAvailable(userProfileSettingDto.Email);
        if (!isAvailable)
            throw new AppException(Constants.EMAIL_ALREADY_IN_USE);

        var otp = await commonService.GenerateOtp(user.Email);

        var placeholders = new Dictionary<string, string>
        {
            { "{{user}}", user.FullName ?? user.Email },
            { "{{email}}", userProfileSettingDto.Email },
            { "{{otp}}", otp }
        };

        var responseMessage = await commonService.SendEmailFromTemplate(new TemplatedEmailRequestDto
        {
            ToEmail = userProfileSettingDto.Email,
            TemplateType = EmailTemplateType.EmailVerification,
            Placeholders = placeholders
        });

        return responseMessage;
    }
    #endregion

    #region VerifyOtp
    public async Task<bool> VerifyOtp(VerifyOtpRequestDto request)
    {
        var user = await userRepository.GetAsync(u => u.Id == UserId && !u.IsDeleted)
                   ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, UserId));

        if (string.IsNullOrEmpty(user.Otp) || !user.OtpSentDate.HasValue)
            throw new AppException(Constants.OTP_NOT_GENERATED_OR_EXPIRED);

        // check OTP validity (5 minutes)
        if (user.OtpSentDate.Value.AddMinutes(5) < DateTime.UtcNow)
            throw new AppException(Constants.OTP_EXPIRED);

        if (!user.Otp.Equals(request.Otp))
            throw new AppException(Constants.OTP_INVALID);

        return true;
    }

    #endregion

    #region Get Admin Profile
    public async Task<AdminProfileResponseDto> GetAdminProfile()
    {
        User user = await userRepository.GetAsync(u => u.Id == UserId && !u.IsDeleted)
                           ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, UserId));

        return mapper.Map<AdminProfileResponseDto>(user);
    }
    #endregion
    #region Update Admin Profile
    public async Task<CreateUpdateResponseDto> UpdateAdminProfile(AdminProfileRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppException(Constants.FULLNAME_REQUIRED);

        // Check if username exists for another user
        if (await userRepository.Exists(u => u.UserName == request.UserName && u.Id != UserId))
            throw new AppException(Constants.DUPLICATE_USERNAME);

        // Check if email exists for another user

        await IsEmailAvailable(request.Email);

        User userEntity = await userRepository.GetAsync(u => u.Id == UserId) ?? throw new AppException(Constants.USER_NOT_FOUND);

        userEntity.FullName = request.FullName.Trim();
        userEntity.UserName = request.UserName.Trim();
        userEntity.Email = request.Email.Trim();
        userEntity.Bio = request.Bio;

        // Save changes
        await userRepository.UpdateAsync(userEntity);

        return new CreateUpdateResponseDto
        {
            Success = true,
            Message = Constants.PROFILE_UPDATED_SUCCESSFULLY
        };
    }

    #endregion
}
