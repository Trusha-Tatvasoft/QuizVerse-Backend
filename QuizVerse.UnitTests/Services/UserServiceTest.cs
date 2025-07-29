using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class UserServiceTest
{
    private readonly Mock<IGenericRepository<User>> _userRepoMock = new();
    private readonly Mock<ICommonService> _commonServiceMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly UserService _service;

    public UserServiceTest()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("userId", "1")
            ]))
        };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _service = new UserService(
            _userRepoMock.Object,
            _commonServiceMock.Object,
            _configMock.Object,
            _emailServiceMock.Object,
            _mapperMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    [Fact]
    public async Task GetUserById_WorksAsExpected()
    {
        var user = new User { Id = 1 };
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(new UserDto { Id = 1 });

        var result = await _service.GetUserById(1);
        Assert.Equal(1, result.Id);

        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User)null!);
        await Assert.ThrowsAsync<AppException>(() => _service.GetUserById(99));
    }

    [Theory]
    [InlineData("test@example.com", "testuser", "", true)]
    [InlineData("test@example.com", "testuser", "123", false)]
    public async Task CreateUser_ValidatesFieldsCorrectly(string email, string username, string password, bool shouldThrow)
    {
        var dto = new UserRequestDto { Email = email, UserName = username, Password = password };
        _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _commonServiceMock.Setup(c => c.Hash(It.IsAny<string>())).Returns("hashed");
        _mapperMock.Setup(m => m.Map<User>(dto)).Returns(new User());
        _configMock.Setup(c => c["EmailSettings:NewUserTemplatePath"]).Returns("template.txt");
        File.WriteAllText("template.txt", "Hi {username}, {password}");
        _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>())).ReturnsAsync(true);

        if (shouldThrow)
        {
            await Assert.ThrowsAsync<AppException>(() => _service.CreateOrUpdateUser(dto));
        }
        else
        {
            var result = await _service.CreateOrUpdateUser(dto);
            Assert.True(result.Success);
            Assert.Contains("email", result.Message.ToLower());
        }
    }

    [Fact]
    public async Task UpdateUser_WorksAndValidatesFields()
    {
        var dto = new UserRequestDto { Id = 1, Email = "updated@example.com", UserName = "updatedUser" };
        var user = new User { Id = 1 };

        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);

        var result = await _service.CreateOrUpdateUser(dto);
        Assert.True(result.Success);
        Assert.Equal(Constants.UPDATE_SUCCESS, result.Message);
    }

    [Fact]
    public async Task UpdateUser_ThrowsIfNotFound()
    {
        var dto = new UserRequestDto { Id = 2 };
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User)null!);
        await Assert.ThrowsAsync<AppException>(() => _service.CreateOrUpdateUser(dto));
    }

    [Theory]
    [InlineData(true, false, Constants.DUPLICATE_EMAIL)]
    [InlineData(false, true, Constants.DUPLICATE_USERNAME)]
    public async Task UpdateUser_Throws_IfDuplicateEmailOrUsername(bool duplicateEmail, bool duplicateUsername, string expectedMessage)
    {
        var dto = new UserRequestDto
        {
            Id = 10,
            Email = "update@example.com",
            UserName = "updateuser"
        };

        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(new User { Id = 10, Email = "old@example.com", UserName = "olduser" });

        _userRepoMock.Setup(r => r.Exists(It.Is<Expression<Func<User, bool>>>(expr =>
            expr.Compile()(new User { Email = dto.Email, Id = 999 })))).ReturnsAsync(duplicateEmail);

        _userRepoMock.Setup(r => r.Exists(It.Is<Expression<Func<User, bool>>>(expr =>
            expr.Compile()(new User { UserName = dto.UserName, Id = 999 })))).ReturnsAsync(duplicateUsername);

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateOrUpdateUser(dto));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory]
    [InlineData(UserActionType.Delete, null, Constants.DELETE_SUCCESS)]
    [InlineData(UserActionType.ChangeStatus, UserStatus.Suspended, "status changed")]
    public async Task UpdateUserByAction_ValidScenarios(UserActionType action, UserStatus? newStatus, string expected)
    {
        var user = new User { Id = 1, IsDeleted = false, Status = (int)UserStatus.Active };
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);

        var req = new UserActionRequest { Id = 1, Action = action, NewStatus = newStatus };
        var result = await _service.UpdateUserByAction(req);

        Assert.Contains(expected.ToLower(), result.ToLower());
    }

    [Theory]
    [InlineData(true, "already deleted")]
    [InlineData(false, "status", true)]
    [InlineData(false, "is already", false, true)]
    public async Task UpdateUserByAction_ThrowsValidationErrors(
        bool isDeleted, string expectedError, bool missingStatus = false, bool sameStatus = false)
    {
        var user = new User { Id = 1, IsDeleted = isDeleted, Status = (int)UserStatus.Active };
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);

        var req = new UserActionRequest
        {
            Id = 1,
            Action = isDeleted ? UserActionType.Delete : UserActionType.ChangeStatus,
            NewStatus = missingStatus ? null : sameStatus ? UserStatus.Active : UserStatus.Suspended
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateUserByAction(req));
        Assert.Contains(expectedError.ToLower(), ex.Message.ToLower());
    }

    [Fact]
    public async Task UpdateUserByAction_ThrowsForInvalidAction()
    {
        var user = new User { Id = 1 };
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateUserByAction(new UserActionRequest { Id = 1, Action = (UserActionType)999 }));
        Assert.Contains("invalid", ex.Message.ToLower());
    }

    [Fact]
    public async Task SendMailToNewUser_Throws_WhenTemplateMissing()
    {
        _configMock.Setup(c => c["EmailSettings:NewUserTemplatePath"]).Returns((string)null!);
        var method = _service.GetType().GetMethod("SendMailToNewUser", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = method!.Invoke(_service, new object[] { "test@example.com", "12345" }) as Task<string>;
        var ex = await Assert.ThrowsAsync<AppException>(() => task!);
        Assert.Contains("template", ex.Message.ToLower());
    }
}
