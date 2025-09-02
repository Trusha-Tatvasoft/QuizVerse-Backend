using AutoMapper;
using Microsoft.AspNetCore.Http;
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
    IHttpContextAccessor httpContextAccessor,
    IEmailService emailService) : IUserProfileService
{
    public int? UserId => httpContextAccessor.HttpContext?.User?.GetUserId();
    // public int UserId = 2;

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
        var normalizedEmail = email.ToLower().Trim();
        var user = await userRepository.GetAsync(u => u.Email.ToLower().Trim() == normalizedEmail);

        if (user == null)
            return true;

        // If the email belongs to the same user, it's available
        if (UserId.HasValue && user.Id == UserId.Value)
            return true;

        switch ((UserStatus)user.Status)
        {
            case UserStatus.Suspended:
                throw new AppException(Constants.EMAIL_SUSPENDED);

            case UserStatus.Active:
            case UserStatus.Inactive:
                if (user.IsDeleted)
                    return true;
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

        var responseMessage = await SendOtpEmail(userProfileSettingDto.Email, otp);

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

    #region SendOtpEmail
    private async Task<string> SendOtpEmail(string email, string otp)
    {
        string? templatePath = Constants.OTP_TEMPLATE_PATH;
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new AppException(Constants.EMAIL_PATH_NOT_CONFIGURED);

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);
        string emailBody = await File.ReadAllTextAsync(fullPath);

        emailBody = emailBody.Replace("{username}", email);
        emailBody = emailBody.Replace("{otp}", otp);

        bool isEmailSent = await emailService.SendEmailAsync(new EmailRequestDto
        {
            To = email,
            Subject = Constants.EMAIL_OTP_SUBJECT,
            Body = emailBody
        });

        if (isEmailSent)
            return string.Format(Constants.EMAIL_SENT_SUCCESS, email);
        else
            return Constants.EMAIL_NOT_SENT;
    }
    #endregion
}
