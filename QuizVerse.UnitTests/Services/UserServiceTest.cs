using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Data;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Repository;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class UserServiceTests
{
    private readonly QuizVerseDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserService _userService;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ICommonService> _commonServiceMock;
    private readonly Mock<IConfiguration> _configMock;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new QuizVerseDbContext(options);
        SeedTestData();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserDto>().ReverseMap();
            cfg.CreateMap<UserRequestDto, User>().ReverseMap();
        });
        _mapper = mapperConfig.CreateMapper();

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim("userId", "1")
        }));
        httpContextAccessor.Setup(x => x.HttpContext.User).Returns(user);

        _emailServiceMock = new Mock<IEmailService>();
        _commonServiceMock = new Mock<ICommonService>();
        _commonServiceMock.Setup(s => s.Hash(It.IsAny<string>())).Returns("hashedPassword");

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["EmailSettings:NewUserTemplatePath"]).Returns("email-template.txt");

        File.WriteAllText("email-template.txt", "Hello {username}, your password is {password}");

        var userRepository = new GenericRepository<User>(_context);
        _userService = new UserService(userRepository, _commonServiceMock.Object, _configMock.Object,
            _emailServiceMock.Object, _mapper, httpContextAccessor.Object);
    }

    #region SeedData
    private void SeedTestData()
    {
        var role = new UserRole { Id = 1, Name = "Player" };
        _context.UserRoles.Add(role);

        _context.Users.AddRange(
            new User
            {
                Id = 1,
                FullName = "Alice Johnson",
                Email = "alice@example.com",
                Password = "samplePass1",
                UserName = "alice",
                Status = (int)UserStatus.Active,
                RoleId = 1,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                FullName = "Bob Smith",
                Email = "bob@example.com",
                Password = "samplePass2",
                UserName = "bob",
                Status = (int)UserStatus.Suspended,
                RoleId = 1,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            }
        );

        _context.SaveChanges();
    }
    #endregion

    #region GetAllUserData 
    [Fact]
    public async Task GetUsersList_WithSearchFilterSort_ReturnsCorrectData()
    {
        var query = new PagedQueryDto
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "alice",
            SortColumn = "fullname",
            SortDescending = false
        };

        var result = await _userService.GetUsersList(query);

        Assert.Single(result.Records);
        Assert.Equal("Alice Johnson", result.Records.First().FullName);
    }

    [Fact]
    public async Task GetUsersList_SortDescendingByEmail_WorksCorrectly()
    {
        var query = new PagedQueryDto
        {
            PageNumber = 1,
            PageSize = 10,
            SortColumn = "fullname",
            SortDescending = true
        };

        var result = await _userService.GetUsersList(query);

        Assert.Equal(2, result.Records.Count());
        Assert.Equal("bob@example.com", result.Records.First().Email);
    }

    [Fact]
    public async Task GetUsersList_NoMatchingSearch_ReturnsZeroRecords()
    {
        var query = new PagedQueryDto
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "nonexistentuser"
        };

        var result = await _userService.GetUsersList(query);

        Assert.Empty(result.Records);
        Assert.Equal(0, result.TotalRecords);
    }

    [Fact]
    public async Task GetUsersList_WithInvalidStatusFilter_ThrowsAppException()
    {
        var query = new PagedQueryDto
        {
            PageNumber = 1,
            PageSize = 10,
            Filters = new Dictionary<string, string> { { "status", "invalid" } }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.GetUsersList(query));
        Assert.Equal(Constants.INVALID_STATUS_MESSAGE, ex.Message);
    }

    [Fact]
    public async Task GetUsersList_WithInvalidRoleFilter_ThrowsAppException()
    {
        var query = new PagedQueryDto
        {
            PageNumber = 1,
            PageSize = 10,
            Filters = new Dictionary<string, string> { { "role", "999" } }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.GetUsersList(query));
        Assert.Equal(Constants.INVALID_ROLE_MESSAGE, ex.Message);
    }

    [Fact]
    public async Task GetUsersList_ValidStatusFilter_AppliesFilter()
    {
        var query = new PagedQueryDto
        {
            Filters = new Dictionary<string, string>
        {
            { "status", ((int)UserStatus.Active).ToString() }
        }
        };

        var result = await _userService.GetUsersList(query);

        Assert.All(result.Records, u => Assert.Equal((int)UserStatus.Active, u.Status));
    }

    [Fact]
    public async Task GetUsersList_InvalidStatus_ThrowsAppException()
    {
        var query = new PagedQueryDto
        {
            Filters = new Dictionary<string, string>
        {
            { "status", "999" }
        }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.GetUsersList(query));
        Assert.Equal(Constants.INVALID_STATUS_MESSAGE, ex.Message);
    }

    [Fact]
    public async Task GetUsersList_ValidRoleFilter_AppliesFilter()
    {
        var query = new PagedQueryDto
        {
            Filters = new Dictionary<string, string>
        {
            { "role", ((int)UserRoles.Player).ToString() }
        }
        };

        var result = await _userService.GetUsersList(query);

        Assert.All(result.Records, u => Assert.Equal((int)UserRoles.Player, u.RoleId));
    }

    [Fact]
    public async Task GetUsersList_InvalidRole_ThrowsAppException()
    {
        var query = new PagedQueryDto
        {
            Filters = new Dictionary<string, string>
        {
            { "role", "777" } // Not defined in UserRoles
        }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.GetUsersList(query));
        Assert.Equal(Constants.INVALID_ROLE_MESSAGE, ex.Message);
    }
    #endregion

    #region GetUserById
    [Fact]
    public async Task GetUserById_ValidId_ReturnsUser()
    {
        var result = await _userService.GetUserById(1);

        Assert.NotNull(result);
        Assert.Equal("Alice Johnson", result.FullName);
    }

    [Fact]
    public async Task GetUserById_InvalidId_ThrowsAppException()
    {
        await Assert.ThrowsAsync<AppException>(() => _userService.GetUserById(999));
    }
    #endregion

    #region CreateUser
    [Fact]
    public async Task CreateUser_ValidData_Success()
    {
        var newUser = new UserRequestDto
        {
            FullName = "Charlie Test",
            Email = "charlie@example.com",
            UserName = "charlie",
            Password = "password123"
        };

        _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>())).ReturnsAsync(true);

        var (success, message) = await _userService.CreateOrUpdateUser(newUser);

        Assert.True(success);
        Assert.Contains("created", message.ToLower());
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ThrowsAppException()
    {
        var dto = new UserRequestDto
        {
            Email = "alice@example.com",
            UserName = "newuser",
            Password = "1234"
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.DUPLICATE_EMAIL, ex.Message);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ThrowsAppException()
    {
        var dto = new UserRequestDto
        {
            Email = "newemail@example.com",
            UserName = "alice",
            Password = "1234"
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.DUPLICATE_USERNAME, ex.Message);
    }

    [Fact]
    public async Task CreateUser_WithoutPassword_ThrowsAppException()
    {
        var dto = new UserRequestDto
        {
            Email = "someone@example.com",
            UserName = "someone"
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.PASSWORD_REQUIRED_FOR_NEW_USER, ex.Message);
    }

    [Fact]
    public async Task CreateUser_MissingEmailTemplatePath_ThrowsAppException()
    {
        _configMock.Setup(c => c["EmailSettings:NewUserTemplatePath"]).Returns(string.Empty);

        var newUser = new UserRequestDto
        {
            FullName = "Template Fail",
            Email = "template@example.com",
            UserName = "templateUser",
            Password = "password"
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(newUser));
        Assert.Equal(Constants.EMAIL_PATH_NOT_CONFIGURED, ex.Message);
    }
    #endregion

    #region UpdateUser
    [Fact]
    public async Task UpdateUser_ValidId_UpdatesSuccessfully()
    {
        var trackedUser = _context.Users.First(u => u.Id == 1);
        _context.Entry(trackedUser).State = EntityState.Detached;

        var updateUser = new UserRequestDto
        {
            Id = 1,
            FullName = "Updated Alice",
            Email = "alice@example.com",
            UserName = "alice"
        };

        var (success, message) = await _userService.CreateOrUpdateUser(updateUser);

        Assert.True(success);
        Assert.Contains("updated", message.ToLower());
    }

    [Fact]
    public async Task UpdateUser_DuplicateEmail_ThrowsAppException()
    {
        // Arrange
        var existingUser = _context.Users.First();
        var anotherUser = _context.Users.First(u => u.Id != existingUser.Id);

        var dto = new UserRequestDto
        {
            Id = existingUser.Id,
            Email = anotherUser.Email,
            UserName = existingUser.UserName,
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.DUPLICATE_EMAIL, ex.Message);
    }
    #endregion

    #region UpdateUserByAction
    [Fact]
    public async Task UpdateUser_DuplicateUsername_ThrowsAppException()
    {
        // Arrange
        var existingUser = _context.Users.First();
        var anotherUser = _context.Users.First(u => u.Id != existingUser.Id);

        var dto = new UserRequestDto
        {
            Id = existingUser.Id,
            Email = existingUser.Email,
            UserName = anotherUser.UserName
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.DUPLICATE_USERNAME, ex.Message);
    }


    [Fact]
    public async Task UpdateUserByAction_Delete_Success()
    {
        var trackedUser = _context.Users.First(u => u.Id == 1);
        _context.Entry(trackedUser).State = EntityState.Detached;

        var actionRequest = new UserActionRequest
        {
            Id = 1,
            Action = UserActionType.Delete
        };

        var message = await _userService.UpdateUserByAction(actionRequest);

        Assert.Contains("deleted", message.ToLower());
    }

    [Fact]
    public async Task UpdateUserByAction_ChangeStatus_Success()
    {
        var trackedUser = _context.Users.First(u => u.Id == 2);
        _context.Entry(trackedUser).State = EntityState.Detached;

        var actionRequest = new UserActionRequest
        {
            Id = 2,
            Action = UserActionType.ChangeStatus,
            NewStatus = UserStatus.Active
        };

        var message = await _userService.UpdateUserByAction(actionRequest);

        Assert.Contains("changed", message.ToLower());
    }

    [Fact]
    public async Task UpdateUserByAction_StatusAlreadySet_ThrowsAppException()
    {
        var existingUser = _context.Users.First();
        _context.Entry(existingUser).State = EntityState.Detached;

        var request = new UserActionRequest
        {
            Id = existingUser.Id,
            Action = UserActionType.ChangeStatus,
            NewStatus = (UserStatus)existingUser.Status
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.UpdateUserByAction(request));

        var expectedMessage = string.Format(Constants.STATUS_ALREADY_SET, request.NewStatus);
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task UpdateUserByAction_NullStatus_ThrowsAppException()
    {
        var user = _context.Users.First(u => !u.IsDeleted);
        _context.Entry(user).State = EntityState.Detached;

        var request = new UserActionRequest
        {
            Id = user.Id,
            Action = UserActionType.ChangeStatus,
            NewStatus = null
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.UpdateUserByAction(request));
        Assert.Equal(Constants.STATUS_REQUIRED, ex.Message);
    }
    #endregion
}
