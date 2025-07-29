using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class UserService(IGenericRepository<User> userRepository, ICommonService commonService, IConfiguration config, IEmailService emailService, IMapper mapper, IHttpContextAccessor httpContextAccessor) : IUserService
{
    public int? UserId => httpContextAccessor.HttpContext?.User?.GetUserId();

    #region GetUserById
    public async Task<UserDto> GetUserById(int id)
    {
        var user = await userRepository.GetAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, id));

        return mapper.Map<UserDto>(user);
    }
    #endregion

    #region Create Or Update
    public async Task<(bool Success, string Message)> CreateOrUpdateUser(UserRequestDto dto)
    {
        if (dto.Id.HasValue && dto.Id.Value > 0)
        {
            // UPDATE
            var user = await userRepository.GetAsync(u => u.Id == dto.Id && !u.IsDeleted)
                ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, dto.Id));

            if (await userRepository.Exists(u => u.Email == dto.Email && u.Id != dto.Id))
                throw new AppException(Constants.DUPLICATE_EMAIL);

            if (await userRepository.Exists(u => u.UserName == dto.UserName && u.Id != dto.Id))
                throw new AppException(Constants.DUPLICATE_USERNAME);

            mapper.Map(dto, user);
            user.ModifiedBy = UserId;
            user.ModifiedDate = DateTime.UtcNow;

            await userRepository.UpdateAsync(user);
            return (true, Constants.UPDATE_SUCCESS);
        }
        else
        {
            // CREATE
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new AppException(Constants.PASSWORD_REQUIRED_FOR_NEW_USER);

            if (await userRepository.Exists(u => u.Email == dto.Email))
                throw new AppException(Constants.DUPLICATE_EMAIL);

            if (await userRepository.Exists(u => u.UserName == dto.UserName))
                throw new AppException(Constants.DUPLICATE_USERNAME);

            var user = mapper.Map<User>(dto);
            user.Password = commonService.Hash(dto.Password);
            user.RoleId = (int)UserRoles.Player;
            user.Status = (int)UserStatus.Active;
            user.CreatedDate = DateTime.UtcNow;
            user.CreatedBy = UserId;
            user.FirstTimeLogin = true;
            user.LastLogin = DateTime.UtcNow;

            await userRepository.AddAsync(user);

            string emailStatus = await SendMailToNewUser(user.Email, dto.Password);

            return (true, string.Format(Constants.CREATE_SUCCESS + ". " + emailStatus));
        }
    }
    #endregion

    #region Update by Action
    public async Task<string> UpdateUserByAction(UserActionRequest userActionRequest)
    {
        var user = await userRepository.GetAsync(u => u.Id == userActionRequest.Id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.USER_NOT_FOUND, userActionRequest.Id));

        string resultMessage;

        switch (userActionRequest.Action)
        {
            case UserActionType.Delete:
                if (user.IsDeleted)
                    throw new AppException(string.Format(Constants.USER_ALREADY_DELETED, userActionRequest.Id));

                user.IsDeleted = true;
                user.ModifiedBy = UserId;
                user.ModifiedDate = DateTime.UtcNow;
                resultMessage = Constants.DELETE_SUCCESS;
                break;

            case UserActionType.ChangeStatus:
                if (userActionRequest.NewStatus is null)
                    throw new AppException(Constants.STATUS_REQUIRED);

                if (user.Status == (int)userActionRequest.NewStatus)
                    throw new AppException(string.Format(Constants.STATUS_ALREADY_SET, userActionRequest.NewStatus));

                user.Status = (int)userActionRequest.NewStatus;
                user.ModifiedBy = UserId;
                user.ModifiedDate = DateTime.UtcNow;
                resultMessage = string.Format(Constants.USER_STATUS_CHANGED_SUCCESS, user.Id, userActionRequest.NewStatus);
                break;

            default:
                throw new AppException(Constants.INVALID_DATA_MESSAGE);
        }

        await userRepository.UpdateAsync(user);

        return resultMessage;
    }
    #endregion

    #region Send mail
    private async Task<string> SendMailToNewUser(string email, string plainPassword)
    {
        string? templatePath = config["EmailSettings:NewUserTemplatePath"];
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new AppException(Constants.EMAIL_PATH_NOT_CONFIGURED);

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);
        string emailBody = await File.ReadAllTextAsync(fullPath);

        emailBody = emailBody.Replace("{username}", email);
        emailBody = emailBody.Replace("{password}", plainPassword);

        bool isEmailSent = await emailService.SendEmailAsync(new EmailRequestDto
        {
            To = email,
            Subject = Constants.QUIZVERSE_DEFAULT_QUOTE,
            Body = emailBody,
            Cc = ["cc1@mail.com", "cc2@mail.com"],
            Bcc = ["bcc1@mail.com", "bcc2@mail.com"]
        });

        if (isEmailSent)
            return string.Format(Constants.EMAIL_SENT_SUCCESS, email);
        else
            return Constants.EMAIL_NOT_SENT;

    }
    #endregion
}
