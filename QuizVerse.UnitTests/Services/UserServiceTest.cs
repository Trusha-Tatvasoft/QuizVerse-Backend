using System.Security.Claims;
using AutoMapper;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Data;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Mappings;
using QuizVerse.Infrastructure.Repository;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class UserServiceTests
{
    private readonly QuizVerseDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserService _userService;
    private readonly Mock<ICommonService> _commonServiceMock;
    private readonly Mock<ISqlQueryRepository> _sqlQueryRepositoryMock;

    public UserServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
            {
                {"QuizVerse:LoginUrl", "http://quizverse-frontend.web2.anasource.com/login"},
            };

        IConfiguration configuration = new ConfigurationBuilder()
             .AddInMemoryCollection(inMemorySettings!)
             .Build();

        var options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new QuizVerseDbContext(options);
        SeedTestData();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.UserData, "1") }, "mock"));
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        _commonServiceMock = new Mock<ICommonService>();
        _commonServiceMock.Setup(s => s.Hash(It.IsAny<string>())).Returns("hashedPassword");

        _sqlQueryRepositoryMock = new Mock<ISqlQueryRepository>();

        // Setup for duplicate email failure
        _sqlQueryRepositoryMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.Is<NpgsqlParameter[]>(p => p.Any(param => param.ParameterName == "p_email" && (string)param.Value == "alice@example.com"))))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = false,
                Message = Constants.DUPLICATE_EMAIL
            });

        // Setup for duplicate username failure
        _sqlQueryRepositoryMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.Is<NpgsqlParameter[]>(p => p.Any(param => param.ParameterName == "p_username" && (string)param.Value == "alice"))))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = false,
                Message = Constants.DUPLICATE_USERNAME
            });



        var userRepository = new GenericRepository<User>(_context);

        _userService = new UserService(
            userRepository,
            _commonServiceMock.Object,
            _mapper,
            httpContextAccessor.Object,
            _sqlQueryRepositoryMock.Object,
            configuration
        );
    }

    #region SeedData
    private void SeedTestData()
    {
        var roleId = (int)UserRoles.Player;

        var role = _context.UserRoles.FirstOrDefault(r => r.Id == roleId);
        if (role == null)
        {
            role = new UserRole { Id = roleId, Name = "Player" };
            _context.UserRoles.Add(role);
            _context.SaveChanges();
        }

        if (!_context.Users.Any())
        {
            _context.Users.AddRange(
                new User
                {
                    FullName = "Alice Johnson",
                    Email = "alice@example.com",
                    Password = "samplePass1",
                    UserName = "alice",
                    Status = (int)UserStatus.Active,
                    RoleId = roleId,
                    Role = role,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow,
                    QuizAttempteds = new List<QuizAttempted>()
                },
                new User
                {
                    FullName = "Bob Smith",
                    Email = "bob@example.com",
                    Password = "samplePass2",
                    UserName = "bob",
                    Status = (int)UserStatus.Suspended,
                    RoleId = roleId,
                    Role = role,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow,
                    QuizAttempteds = new List<QuizAttempted>()
                }
            );
            _context.SaveChanges();
        }
    }
    #endregion

    #region GetAllUserData 
    [Fact]
    public async Task GetUsersList_WithSearchFilterSort_ReturnsCorrectData()
    {
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "alice",
            SortColumn = "fullname",
            SortDescending = false
        };

        var result = await _userService.GetUsersByPagination(query);

        Assert.Single(result.Records);
        Assert.Equal("Alice Johnson", result.Records.First().FullName);
    }

    [Fact]
    public async Task GetUsersList_SortDescendingByEmail_WorksCorrectly()
    {
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortColumn = "fullname",
            SortDescending = true
        };

        var result = await _userService.GetUsersByPagination(query);

        Assert.Equal(2, result.Records.Count());
        Assert.Equal("bob@example.com", result.Records.First().Email);
    }

    [Fact]
    public async Task GetUsersList_NoMatchingSearch_ReturnsZeroRecords()
    {
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "nonexistentuser"
        };

        var result = await _userService.GetUsersByPagination(query);

        Assert.Empty(result.Records);
        Assert.Equal(0, result.TotalRecords);
    }

    [Fact]
    public async Task GetUsersList_WithInvalidStatusFilter_ThrowsAppException()
    {
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Filters = new FilterDto
            {
                Status = (UserStatus)999
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.GetUsersByPagination(query));
        Assert.Equal(Constants.INVALID_STATUS_MESSAGE, ex.Message);
    }

    [Fact]
    public async Task GetUsersList_WithInvalidRoleFilter_ThrowsAppException()
    {
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Filters = new FilterDto
            {
                Role = (UserRoles)999
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.GetUsersByPagination(query));
        Assert.Equal(Constants.INVALID_ROLE_MESSAGE, ex.Message);
    }

    [Fact]
    public async Task GetUsersList_ValidStatusFilter_AppliesFilter()
    {
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Filters = new FilterDto
            {
                Status = UserStatus.Active
            }
        };

        var result = await _userService.GetUsersByPagination(query);

        Assert.All(result.Records, u => Assert.Equal((int)UserStatus.Active, u.Status));
    }

    [Fact]
    public async Task GetUsersList_ValidRoleFilter_AppliesFilter()
    {
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Filters = new FilterDto
            {
                Role = UserRoles.Player
            }
        };

        var result = await _userService.GetUsersByPagination(query);

        Assert.All(result.Records, u => Assert.Equal((int)UserRoles.Player, u.RoleId));
    }

    [Fact]
    public async Task GetUsersList_WithInvalidRoleEnum_ThrowsAppException()
    {
        var query = new PageListRequest
        {
            Filters = new FilterDto
            {
                Role = (UserRoles)777
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.GetUsersByPagination(query));
        Assert.Equal(Constants.INVALID_ROLE_MESSAGE, ex.Message);
    }

    [Fact]
    public async Task GetUsersList_WithInvalidStatusEnum_ThrowsAppException()
    {
        var query = new PageListRequest
        {
            Filters = new FilterDto
            {
                Status = (UserStatus)777
            }
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.GetUsersByPagination(query));
        Assert.Equal(Constants.INVALID_STATUS_MESSAGE, ex.Message);
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
    public async Task CreateUser_AdminCreatedUser_SendsNewUserEmail()
    {
        var dto = new UserRequestDto
        {
            FullName = "Test User",
            Email = "testuser@example.com",
            UserName = "testuser",
            Password = "Test@123",
            IsRegister = false
        };

        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
            .ReturnsAsync($"Email successfully sent to {dto.Email}");

        _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
            It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = true,
                Message = "User created successfully."
            });

        var (Success, Message) = await _userService.CreateOrUpdateUser(dto);

        Assert.True(Success);
        Assert.Contains(Constants.USER_CREATE_SUCCESS, Message);

        _commonServiceMock.Verify(s =>
            s.SendEmailFromTemplate(It.Is<TemplatedEmailRequestDto>(email =>
                email.TemplateType == EmailTemplateType.NewUser &&
                email.ToEmail == dto.Email &&
                email.Placeholders["{{user}}"] == dto.Email &&
                email.Placeholders["{{password}}"] == dto.Password &&
                email.Placeholders.ContainsKey("{{loginUrl}}")
            )), Times.Once);
    }

    [Fact]
    public async Task CreateUser_RegisterUser_SendsWelcomeEmail()
    {
        var dto = new UserRequestDto
        {
            FullName = "Test User",
            Email = "testuser1@example.com",
            UserName = "testuser",
            Password = "Test@123",
            IsRegister = true
        };

        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
            .ReturnsAsync(string.Format(Constants.EMAIL_SENT_SUCCESS, dto.Email));

        _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
            It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = true,
                Message = "User created successfully."
            });

        var (Success, Message) = await _userService.CreateOrUpdateUser(dto);

        Assert.True(Success);
        Assert.Contains(Constants.USER_REGISTERED_AND_EMAIL_SENT, Message);

        _commonServiceMock.Verify(s =>
            s.SendEmailFromTemplate(It.Is<TemplatedEmailRequestDto>(email =>
                email.TemplateType == EmailTemplateType.WelComeEmail &&
                email.ToEmail == dto.Email &&
                email.Placeholders["{{user}}"] == dto.Email &&
                email.Placeholders["{{email}}"] == dto.Email &&
                email.Placeholders.ContainsKey("{{registrationDate}}") &&
                email.Placeholders.ContainsKey("{{companyName}}") &&
                email.Placeholders.ContainsKey("{{year}}") &&
                email.Placeholders.ContainsKey("{{loginUrl}}")
            )), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithProfilePic_SavesFile()
    {
        var fileMock = new Mock<IFormFile>();
        var content = "fake image content";
        var fileName = "test.jpg";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(stream.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var dto = new UserRequestDto
        {
            FullName = "User",
            Email = "imageuser@example.com",
            UserName = "imageuser",
            Password = "pass123",
            ProfilePic = fileMock.Object,
            IsRegister = false
        };

        _commonServiceMock
            .Setup(s => s.SaveFile(dto.ProfilePic, "users"))
            .ReturnsAsync("uploads/users/test.jpg");

        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
            .ReturnsAsync("Email sent");

        _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
            It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = true,
                Message = "User created successfully."
            });

        var (Success, Message) = await _userService.CreateOrUpdateUser(dto);

        Assert.True(Success);
        _commonServiceMock.Verify(s => s.SaveFile(dto.ProfilePic, "users"), Times.Once);
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
    public async Task CreateUser_RegisterEmailFails_ThrowsException()
    {
        var dto = new UserRequestDto
        {
            FullName = "Test User",
            Email = "failuser@example.com",
            UserName = "failuser",
            Password = "Test@123",
            IsRegister = true
        };

        _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
            It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = true,
                Message = "User created successfully."
            });

        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
            .ReturnsAsync("Some failure message");

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));

        Assert.Equal(Constants.USER_REGISTERED_BUT_EMAIL_NOT_SENT, ex.Message);
    }

    #endregion

    #region UpdateUser

    [Fact]
    public async Task UpdateUser_WithNewProfilePic_UpdatesImagePath()
    {
        var existingUserId = 1;
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("newpic.jpg");
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[100]));

        var dto = new UserRequestDto
        {
            Id = existingUserId,
            FullName = "Updated Name",
            Email = "user@example.com",
            UserName = "updatedusername",
            ProfilePic = fileMock.Object,
            Bio = "Updated bio",
            Password = null // no password change
        };

        _commonServiceMock
            .Setup(s => s.SaveFile(dto.ProfilePic, "users"))
            .ReturnsAsync("users/newpic.jpg");

        _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
    It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
    .ReturnsAsync(new CreateUpdateResponseDto
    {
        Success = true,
        Message = "User updated successfully."
    });

        var (Success, Message) = await _userService.CreateOrUpdateUser(dto);

        Assert.True(Success);

        _commonServiceMock.Verify(s => s.SaveFile(dto.ProfilePic, "users"), Times.Once);
        _sqlQueryRepositoryMock.Verify(s =>
            s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_UserNotFound_ThrowsException()
    {
        var dto = new UserRequestDto
        {
            Id = 99,
            FullName = "Name",
            Email = "user@example.com",
            UserName = "username",
            ProfilePic = null,
            Password = null
        };

        _sqlQueryRepositoryMock.Setup(s =>
            s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = false,
                Message = "User with ID 99 not found."
            });

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal("User with ID 99 not found.", ex.Message);
    }

    [Fact]
    public async Task UpdateUser_EmailChanged_ThrowsException()
    {
        // Arrange
        var dto = new UserRequestDto
        {
            Id = 1,
            FullName = "Name",
            Email = "newemail@example.com",
            UserName = "username",
            ProfilePic = null,
            Password = null
        };

        _sqlQueryRepositoryMock.Setup(s =>
            s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = false,
                Message = "Email can't be changed"
            });

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal("Email can't be changed", ex.Message);
    }

    [Fact]
    public async Task UpdateUser_DuplicateUsername_ThrowsException()
    {
        // Arrange
        var dto = new UserRequestDto
        {
            Id = 1,
            FullName = "Name",
            Email = "user@example.com",
            UserName = "duplicateusername",
            ProfilePic = null,
            Password = null
        };

        _sqlQueryRepositoryMock.Setup(s =>
            s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = false,
                Message = "User with this username already exists."
            });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal("User with this username already exists.", ex.Message);
    }

    [Fact]
    public async Task UpdateUser_DoesNotSendEmail()
    {
        var dto = new UserRequestDto
        {
            Id = 1,
            FullName = "Updated Name",
            Email = "user@example.com",
            UserName = "updateduser",
            Password = null,
            IsRegister = false
        };

        _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
            It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = true,
                Message = "User updated successfully."
            });

        var (Success, Message) = await _userService.CreateOrUpdateUser(dto);

        Assert.True(Success);
        Assert.Equal("User updated successfully.", Message);
        _commonServiceMock.Verify(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()), Times.Never);
    }

    #endregion

    #region UpdateUserByAction
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
    [Fact]
    public async Task UpdateUserByAction_ChangeStatus_ToSuspended_SendsSuspensionEmail()
    {
        // Arrange
        var user = _context.Users.First(u => u.Status != (int)UserStatus.Suspended);
        _context.Entry(user).State = EntityState.Detached;

        var actionRequest = new UserActionRequest
        {
            Id = user.Id,
            Action = UserActionType.ChangeStatus,
            NewStatus = UserStatus.Suspended
        };

        // Setup mock to verify email sending
        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.Is<TemplatedEmailRequestDto>(dto =>
                dto.TemplateType == EmailTemplateType.AccountSuspension &&
                dto.ToEmail == user.Email &&
                dto.Placeholders.ContainsKey("{{user}}") &&
                dto.Placeholders.ContainsKey("{{email}}")
            )))
            .ReturnsAsync("Email Sent")
            .Verifiable();

        // Act
        var result = await _userService.UpdateUserByAction(actionRequest);

        // Assert
        Assert.Contains("changed", result.ToLower());
        _commonServiceMock.Verify();
    }

    #endregion

    #region ExportUserData

    [Fact]
    public async Task UserExportData_WithValidData_ReturnsExcelStream()
    {
        // Arrange
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10
        };

        var exportData = new List<UserExportDto>
        {
            new() { No = 1, FullName = "Alice Johnson" },
            new() { No = 2, FullName = "Bob Smith" }
        };

        var stream = new MemoryStream();
        _commonServiceMock
            .Setup(x => x.ExportToExcel(
                It.IsAny<List<UserExportDto>>(),
                It.IsAny<string>(),
                It.IsAny<XLTableTheme>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Action<IXLWorksheet>>()))
            .Returns(stream);

        // Act
        var result = await _userService.UserExportData(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(stream, result);
    }

    [Fact]
    public async Task UserExportData_ShouldAddRowNumbers()
    {
        // Arrange
        var query = new PageListRequest();

        var capturedList = new List<UserExportDto>();
        _commonServiceMock
            .Setup(x => x.ExportToExcel(
                It.IsAny<List<UserExportDto>>(),
                It.IsAny<string>(),
                It.IsAny<XLTableTheme>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Action<IXLWorksheet>>()))
            .Callback<List<UserExportDto>, string, XLTableTheme?, int, int, Action<IXLWorksheet>>(
                (list, _, _, _, _, _) => capturedList = list)
            .Returns(new MemoryStream());

        // Act
        var result = await _userService.UserExportData(query);

        // Assert
        Assert.Equal(1, capturedList[0].No);
        Assert.Equal(2, capturedList[1].No);
    }

    [Fact]
    public async Task UserExportData_WhenNoData_ThrowsAppException()
    {
        // Arrange
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();

        var query = new PageListRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.UserExportData(query));
        Assert.Equal(Constants.USER_DATA_NULL, ex.Message);
    }

    [Fact]
    public async Task UserExportData_WithSearchAndFilters_SetsWorksheetMetadata()
    {
        // Arrange
        SeedTestData();

        var query = new PageListRequest
        {
            SearchTerm = "alice",
            Filters = new FilterDto
            {
                Role = UserRoles.Player,
                Status = UserStatus.Active
            }
        };

        Action<IXLWorksheet>? capturedSetup = null;

        _commonServiceMock
            .Setup(x => x.ExportToExcel(
                It.IsAny<List<UserExportDto>>(),
                It.IsAny<string>(),
                It.IsAny<XLTableTheme?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Action<IXLWorksheet>>()))
            .Callback<List<UserExportDto>, string, XLTableTheme?, int, int, Action<IXLWorksheet>>(
                (list, sheet, theme, row, col, setup) =>
                {
                    capturedSetup = setup;
                    Assert.Single(list);
                    Assert.Equal("Alice Johnson", list[0].FullName);
                })
            .Returns(new MemoryStream());

        // Act
        var result = await _userService.UserExportData(query);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(capturedSetup);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("TestSheet");

        capturedSetup.Invoke(worksheet);

        Assert.Equal("Search Text:", worksheet.Cell("A7").Value);
        Assert.Equal("alice", worksheet.Cell("B7").Value);

        Assert.Equal("Total Records:", worksheet.Cell("D7").Value);
        Assert.Equal("1", worksheet.Cell("E7").Value.ToString());

        Assert.Equal("Filter:", worksheet.Cell("G7").Value);
        var filterText = worksheet.Cell("H7").Value.ToString();
        Assert.Contains("Role: Player", filterText);
        Assert.Contains("Status: Active", filterText);
    }

    #endregion


}
