using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class UserService(IUserRepository userRepository, ICustomService customService, IConfiguration config) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ICustomService _customService = customService;
    private readonly IConfiguration _config = config;

    #region GetUserById
    public async Task<UserDto> GetUserById(int id)
    {
        var user = await _userRepository.GetAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.Messages.User.NOT_FOUND, id));

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            UserName = user.UserName,
            RoleId = user.RoleId,
            Status = user.Status,
            JoinedDate = user.CreatedDate,
            Bio = user.Bio,
            ProfilePic = user.ProfilePic
        };
    }
    #endregion

    #region CreateUser
    public async Task<UserDto> CreateUser(CreateUserDto dto)
    {
        var existingUser = await _userRepository.GetAsync(u => u.Email == dto.Email || u.UserName == dto.UserName);

        if (existingUser != null)
        {
            if (existingUser.Email == dto.Email)
                throw new AppException(string.Format(Constants.Messages.User.DUPLICATE_EMAIL));

            if (existingUser.UserName == dto.UserName)
                throw new AppException(string.Format(Constants.Messages.User.DUPLICATE_USERNAME));
        }

        var hashedPassword = _customService.Hash(dto.Password);

        var user = new User
        {
            FullName = dto.FullName,
            UserName = dto.UserName,
            Email = dto.Email,
            Password = hashedPassword,
            RoleId = (int)UserRoles.Player,
            Status = (int)UserStatus.Active,
            CreatedDate = DateTime.UtcNow,
            FirstTimeLogin = true,
            LastLogin = DateTime.UtcNow,
            Bio = dto.Bio,
            ProfilePic = dto.ProfilePic
        };

        await _userRepository.AddAsync(user);

        string templatePath = _config["EmailSettings:NewUserTemplatePath"]!;
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);
        string emailBody = await File.ReadAllTextAsync(fullPath);

        emailBody = emailBody.Replace("{username}", user.Email);
        emailBody = emailBody.Replace("{password}", dto.Password);

        bool emailSent = await _customService.SendEmail(user.Email, "Welcome to QuizVerse!", emailBody);
        string emailStatusMessage = emailSent ? "Email sent successfully." : "Email not sent (sending failed).";

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            UserName = user.UserName,
            RoleId = user.RoleId,
            Status = user.Status,
            JoinedDate = user.CreatedDate,
            Bio = user.Bio,
            ProfilePic = user.ProfilePic,
            EmailStatus = emailStatusMessage
        };
    }
    #endregion

    #region UpdateUser
    public async Task<UserDto> UpdateUser(int id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.Messages.User.NOT_FOUND, id));

        var existingUser = await _userRepository.GetAsync(u => (u.Email == dto.Email || u.UserName == dto.UserName) && u.Id != id);

        if (existingUser != null)
        {
            if (existingUser.Email == dto.Email)
                throw new AppException(string.Format(Constants.Messages.User.DUPLICATE_EMAIL));

            if (existingUser.UserName == dto.UserName)
                throw new AppException(string.Format(Constants.Messages.User.DUPLICATE_USERNAME));
        }

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.UserName = dto.UserName;

        if (!string.IsNullOrWhiteSpace(dto.Bio))
            user.Bio = dto.Bio;

        if (!string.IsNullOrWhiteSpace(dto.ProfilePicture))
            user.ProfilePic = dto.ProfilePicture;

        user.ModifiedDate = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            UserName = user.UserName,
            RoleId = user.RoleId,
            JoinedDate = user.CreatedDate,
            Bio = user.Bio,
            ProfilePic = user.ProfilePic
        };
    }
    #endregion

    #region DeleteUser
    public async Task DeleteUser(int id)
    {
        var user = await _userRepository.GetAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.Messages.User.NOT_FOUND, id));

        user.IsDeleted = true;
        user.ModifiedDate = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }
    #endregion

    #region ChangeUserStatus
    public async Task ChangeUserStatus(int id, UserStatus newStatus)
    {
        var user = await _userRepository.GetAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.Messages.User.NOT_FOUND, id));

        if (user.Status == (int)newStatus)
            throw new AppException(string.Format(Constants.Messages.User.STATUS_ALREADY_SET, newStatus));

        user.Status = (int)newStatus;
        user.ModifiedDate = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }
    #endregion
}
