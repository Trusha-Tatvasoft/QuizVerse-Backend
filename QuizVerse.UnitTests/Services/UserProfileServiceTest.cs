using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class UserProfileServiceTests
{
    private readonly Mock<ISqlQueryRepository> _sqlQueryRepositoryMock = new();
    private readonly Mock<IGenericRepository<User>> _userRepositoryMock = new();
    private readonly Mock<ICommonService> _commonServiceMock = new();
    private readonly Mock<IGenericRepository<Badge>> _badgeRepositoryMock = new();
    private readonly Mock<IGenericRepository<UserBadgesEarned>> _userBadgeRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();

    private readonly UserProfileService _service;

    public UserProfileServiceTests()
    {
        _mapperMock.Setup(m => m.Map<UserProfileSettingDto>(It.IsAny<UserProfileSettingDto>()))
           .Returns((UserProfileSettingDto u) => u);

        var mockClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new("userId", "2")
        }, "mock"));

        var mockHttpContext = new DefaultHttpContext
        {
            User = mockClaimsPrincipal
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(mockHttpContext);

        _service = new UserProfileService(
            _sqlQueryRepositoryMock.Object,
            _userRepositoryMock.Object,
            _commonServiceMock.Object,
            _badgeRepositoryMock.Object,
            _userBadgeRepositoryMock.Object,
            _mapperMock.Object,
            _httpContextAccessorMock.Object,
            _emailServiceMock.Object
        );
    }

    #region GetUserBasicProfile
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetUserBasicProfile_ShouldReturnOrThrow(bool exists)
    {
        var dto = exists ? new UserBasicProfileDto { Name = "Test" } : null!;
        _sqlQueryRepositoryMock
            .Setup(r => r.SqlQuerySingleAsync<UserBasicProfileDto>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(dto);

        if (exists)
        {
            _mapperMock.Setup(m => m.Map<UserBasicProfileDto>(dto)).Returns(dto);
            var result = await _service.GetUserBasicProfile();
            Assert.Equal("Test", result.Name);
        }
        else
        {
            await Assert.ThrowsAsync<AppException>(() => _service.GetUserBasicProfile());
        }
    }
    #endregion

    #region GetUserRecentActivity
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetUserRecentActivity_ShouldReturnOrThrow(bool exists)
    {
        if (exists)
        {
            var recentActivities = new List<RecentActivityDto>
            {
                new() { Type = "Quiz", Description = "Completed Quiz 1", Xp = 50 },
                new() { Type = "Battle", Description = "Won a battle", Xp = 100 }
            };
            var dto = new UserOverviewDto
            {
                RecentActivityJson = JsonSerializer.Serialize(recentActivities),
                GlobalRank = 5,
                BestCategory = "Science",
                LongestStreak = 10
            };
            _sqlQueryRepositoryMock
                .Setup(r => r.SqlQuerySingleAsync<UserOverviewDto>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(dto);

            var result = await _service.GetUserOverview();
            Assert.NotNull(result.RecentActivity);
            Assert.Equal(2, result.RecentActivity.Count);
        }
        else
        {
            _sqlQueryRepositoryMock
                .Setup(r => r.SqlQuerySingleAsync<UserOverviewDto>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync((UserOverviewDto)null!);

            await Assert.ThrowsAsync<AppException>(() => _service.GetUserOverview());
        }
    }
    #endregion

    #region GetUserBadges
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetUserBadges_ShouldReturnCorrectly(bool hasEarned)
    {
        var badges = new List<Badge> { new Badge { Id = 1 } };
        var earned = hasEarned ? new List<UserBadgesEarned> { new UserBadgesEarned { BadgeId = 1, UserId = 2 } } : new List<UserBadgesEarned>();

        _badgeRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Badge, bool>>>()))
            .ReturnsAsync(badges);

        _userBadgeRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserBadgesEarned, bool>>>()))
            .ReturnsAsync(earned);

        var mapped = new List<UserBadgesResponseDto> { new UserBadgesResponseDto { BadgeId = 1 } };
        _mapperMock.Setup(m => m.Map<List<UserBadgesResponseDto>>(badges)).Returns(mapped);

        var result = await _service.GetUserBadges();
        Assert.Equal(hasEarned, result.First().Earned);
    }
    #endregion

    #region GetUserProfileSetting
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetUserProfileSetting_ShouldReturnOrThrow(bool exists)
    {
        if (exists)
        {
            var user = new User { Id = 2, FullName = "Test", IsDeleted = false };
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);
            _mapperMock.Setup(m => m.Map<UserProfileSettingDto>(user)).Returns(new UserProfileSettingDto { FullName = "Test" });

            var result = await _service.GetUserProfileSetting();
            Assert.Equal("Test", result.FullName);
        }
        else
        {
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<AppException>(() => _service.GetUserProfileSetting());
        }
    }
    #endregion

    #region UpdateUserProfile
    [Theory]
    [InlineData("", "test@test.com", true)]
    [InlineData("Test", "used@test.com", true)]
    [InlineData("Test", "new@test.com", false)]
    public async Task UpdateUserProfile_ShouldHandleDifferentScenarios(string fullName, string email, bool shouldThrow)
    {
        var dto = new UserProfileSettingDto { FullName = fullName, Email = email };

        var existingUser = email == "used@test.com"
            ? new User { Email = email, Status = (int)UserStatus.Active, IsDeleted = false }
            : null;

        _userRepositoryMock.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(existingUser);

        _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
            It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto { Message = "Updated" });

        if (shouldThrow)
        {
            await Assert.ThrowsAsync<AppException>(() => _service.UpdateUserProfile(dto));
        }
        else
        {
            var result = await _service.UpdateUserProfile(dto);
            Assert.Equal("Updated", result.Message);
        }
    }

    [Fact]
    public async Task UpdateUserProfile_ShouldCreateCorrectNpgsqlParameters()
    {
        var dto = new UserProfileSettingDto
        {
            FullName = "Valid Name",
            Email = "new@test.com",
            Bio = "Sample Bio"
        };

        _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync((User?)null);

        NpgsqlParameter[]? capturedParams = null;

        _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(), It.IsAny<object[]>()))
            .Callback<string, object[]>((query, parameters) =>
            {
                capturedParams = parameters.Cast<NpgsqlParameter>().ToArray();
            })
            .ReturnsAsync(new CreateUpdateResponseDto { Message = "Updated" });



        var result = await _service.UpdateUserProfile(dto);

        Assert.NotNull(capturedParams);
        Assert.Equal(4, capturedParams!.Length);
        Assert.Equal(dto.Email, capturedParams[1].Value);
        Assert.Equal(dto.FullName, capturedParams[2].Value);
        Assert.Equal(dto.Bio, capturedParams[3].Value);
        Assert.Equal("Updated", result.Message);
    }

    #endregion

    #region SendOtpToUser
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task SendOtpToUser_ShouldBehaveAccordingly(bool emailAvailable, bool emailSent)
    {
        var dto = new UserProfileSettingDto { Email = "test@test.com" };

        var userById = new User { Id = 2, Email = "old@test.com", Status = (int)UserStatus.Active, IsDeleted = false };
        _userRepositoryMock
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(userById);

        _userRepositoryMock
            .SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(userById)
            .ReturnsAsync(emailAvailable ? null : new User { Id = 3, Email = dto.Email, Status = (int)UserStatus.Active, IsDeleted = false });

        _commonServiceMock.Setup(c => c.GenerateOtp(dto.Email)).ReturnsAsync("1234");
        _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>())).ReturnsAsync(emailSent);

        string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "EmailVerifyOTP.html");
        Directory.CreateDirectory(Path.GetDirectoryName(templatePath)!);
        await File.WriteAllTextAsync(templatePath, "Hi {username}, OTP: {otp}");

        try
        {
            if (!emailAvailable)
            {
                var ex = await Assert.ThrowsAsync<AppException>(() => _service.SendOtpToUser(dto));
                Assert.Equal(Constants.EMAIL_ALREADY_IN_USE, ex.Message);
            }
            else if (!emailSent)
            {
                var result = await _service.SendOtpToUser(dto);
                Assert.Equal(Constants.EMAIL_NOT_SENT, result);
            }
            else
            {
                var result = await _service.SendOtpToUser(dto);
                Assert.Contains("email successfully sent", result.ToLower());
            }
        }
        finally
        {
            if (File.Exists(templatePath))
                File.Delete(templatePath);
        }
    }
    #endregion

    #region IsEmailAvailable
    [Theory]
    [InlineData(UserStatus.Active, false, typeof(AppException))]
    [InlineData(UserStatus.Inactive, false, typeof(AppException))]
    [InlineData(UserStatus.Suspended, false, typeof(AppException))]
    [InlineData(UserStatus.Active, true, null)]
    public async Task IsEmailAvailable_ShouldBehaveBasedOnUserStatus(UserStatus status, bool isDeleted, Type? expectedException)
    {
        var user = new User { Email = "test@test.com", Status = (int)status, IsDeleted = isDeleted };
        _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);

        if (expectedException != null)
            await Assert.ThrowsAsync(expectedException, () => _service.IsEmailAvailable("test@test.com"));
        else
        {
            var result = await _service.IsEmailAvailable("test@test.com");
            Assert.True(result);
        }
    }

    [Fact]
    public async Task IsEmailAvailable_ShouldReturnTrue_WhenUserNotFound()
    {
        _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User?)null);
        var result = await _service.IsEmailAvailable("nobody@test.com");
        Assert.True(result);
    }
    #endregion

    #region VerifyOtp
    [Theory]
    [InlineData("1234", "1234", 0, true, null)]
    [InlineData("1234", "5678", 0, false, typeof(AppException))]
    [InlineData("1234", "1234", -10, false, typeof(AppException))]
    [InlineData(null, "1234", 0, false, typeof(AppException))]
    public async Task VerifyOtp_ShouldBehaveAccordingly(string? storedOtp, string requestOtp, int minutesAgo, bool expectedResult, Type? expectedException)
    {
        var user = new User { Id = 2, Otp = storedOtp, OtpSentDate = storedOtp == null ? null : DateTime.UtcNow.AddMinutes(minutesAgo) };
        _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);

        if (expectedException != null)
            await Assert.ThrowsAsync(expectedException, () => _service.VerifyOtp(new VerifyOtpRequestDto { Otp = requestOtp }));
        else
        {
            var result = await _service.VerifyOtp(new VerifyOtpRequestDto { Otp = requestOtp });
            Assert.Equal(expectedResult, result);
        }
    }
    #endregion

    #region UpdateProfilePicture
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateProfilePicture_ShouldHandleUserFoundOrNot(bool userExists)
    {

        var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "ProfilePic", "profile.jpg");

        var dto = new UpdateProfilePicRequestDto { ProfilePic = file };

        if (userExists)
        {
            var user = new User { Id = 2, IsDeleted = false };
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);
            _commonServiceMock.Setup(c => c.SaveFile(dto.ProfilePic, "users")).ReturnsAsync("path/to/file.jpg");
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _service.UpdateProfilePicture(dto);
            Assert.True(result);
            Assert.Equal("path/to/file.jpg", user.ProfilePic);
        }
        else
        {
            _userRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<AppException>(() => _service.UpdateProfilePicture(dto));
        }
    }
    #endregion
}