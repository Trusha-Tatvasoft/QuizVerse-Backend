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
    private readonly IConfiguration _configuration;

    public UserServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"QuizVerse:LoginUrl", "http://quizverse-frontend.web2.anasource.com/login"},
        };

        _configuration = new ConfigurationBuilder()
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

        // Setup HttpContext with UserId = 1 (Alice) as current user
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] 
            { 
                new Claim(ClaimTypes.UserData, "1"),
                new Claim(ClaimTypes.Role, UserRoles.Admin.ToString()) // Admin role
            }, "mock"));
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        _commonServiceMock = new Mock<ICommonService>();
        _sqlQueryRepositoryMock = new Mock<ISqlQueryRepository>();

        // Default hash behavior
        _commonServiceMock.Setup(s => s.Hash(It.IsAny<string>())).Returns("hashedPassword");

        // Setup SQL query repository defaults
        SetupDefaultSqlQueryBehavior();

        var userRepository = new GenericRepository<User>(_context);

        _userService = new UserService(
            userRepository,
            _commonServiceMock.Object,
            _mapper,
            httpContextAccessor.Object,
            _sqlQueryRepositoryMock.Object,
            _configuration
        );
    }

    private void SetupDefaultSqlQueryBehavior()
    {
        // Default success response for create/update
        _sqlQueryRepositoryMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = true,
                Message = Constants.USER_CREATE_SUCCESS
            });

        // Duplicate email failure
        _sqlQueryRepositoryMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.Is<NpgsqlParameter[]>(p => 
                    p.Any(param => param.ParameterName == "p_email" && 
                    param.Value != null && 
                    (string)param.Value == "alice@example.com"))))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = false,
                Message = Constants.DUPLICATE_EMAIL
            });

        // Duplicate username failure
        _sqlQueryRepositoryMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.Is<NpgsqlParameter[]>(p => 
                    p.Any(param => param.ParameterName == "p_username" && 
                    param.Value != null && 
                    (string)param.Value == "alice"))))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = false,
                Message = Constants.DUPLICATE_USERNAME
            });
    }

    #region SeedData
    private void SeedTestData()
    {
        // Clear existing data
        _context.Users.RemoveRange(_context.Users);
        _context.UserRoles.RemoveRange(_context.UserRoles);
        _context.SaveChanges();

        // Add roles
        var adminRole = new UserRole { Id = (int)UserRoles.Admin, Name = "Admin" };
        var playerRole = new UserRole { Id = (int)UserRoles.Player, Name = "Player" };
        _context.UserRoles.AddRange(adminRole, playerRole);
        _context.SaveChanges();

        // Add users
        _context.Users.AddRange(
            new User
            {
                Id = 1,
                FullName = "Alice Johnson",
                Email = "alice@example.com",
                Password = "samplePass1",
                UserName = "alice",
                Status = (int)UserStatus.Active,
                RoleId = (int)UserRoles.Admin,
                Role = adminRole,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow,
                QuizAttempteds = new List<QuizAttempted>()
            },
            new User
            {
                Id = 2,
                FullName = "Bob Smith",
                Email = "bob@example.com",
                Password = "samplePass2",
                UserName = "bob",
                Status = (int)UserStatus.Suspended,
                RoleId = (int)UserRoles.Player,
                Role = playerRole,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow,
                QuizAttempteds = new List<QuizAttempted>()
            },
            new User
            {
                Id = 3,
                FullName = "Charlie Brown",
                Email = "charlie@example.com",
                Password = "samplePass3",
                UserName = "charlie",
                Status = (int)UserStatus.Active,
                RoleId = (int)UserRoles.Player,
                Role = playerRole,
                IsDeleted = false,
                ProfilePic = "old-profile.jpg",
                CreatedDate = DateTime.UtcNow,
                QuizAttempteds = new List<QuizAttempted>()
            }
        );
        _context.SaveChanges();

        // Detach all entities to avoid tracking issues
        _context.ChangeTracker.Clear();
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
            SearchTerm = "bob",
            SortColumn = "fullname",
            SortDescending = false
        };

        var result = await _userService.GetUsersByPagination(query);

        Assert.Single(result.Records);
        Assert.Equal("Bob Smith", result.Records.First().FullName);
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

        // Charlie Brown should be first (descending), but Alice is excluded (current user)
        Assert.Equal(2, result.Records.Count());
        Assert.Equal("Charlie Brown", result.Records.First().FullName);
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
    #endregion

    #region GetUserById
    [Fact]
    public async Task GetUserById_ValidId_ReturnsUser()
    {
        var result = await _userService.GetUserById(2); // Bob

        Assert.NotNull(result);
        Assert.Equal("Bob Smith", result.FullName);
    }

    [Fact]
    public async Task GetUserById_InvalidId_ThrowsAppException()
    {
        await Assert.ThrowsAsync<AppException>(() => _userService.GetUserById(999));
    }
    #endregion

    #region CreateUser

    [Fact]
    public async Task CreateOrUpdateUser_ModifyingSelf_ThrowsAppException()
    {
        var dto = new UserRequestDto 
        { 
            Id = 1, // Current user ID
            Email = "alice@example.com", 
            FullName = "Self User",
            RoleId = (int)UserRoles.Player
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.CANNOT_MODIFY_SELF, ex.Message);
    }

    [Fact]
    public async Task CreateOrUpdateUser_AdminCannotAddSuperAdmin_ThrowsAppException()
    {
        var dto = new UserRequestDto 
        { 
            Id = null, 
            Email = "superadmin@test.com", 
            FullName = "Super Admin User", 
            UserName = "superadmin",
            RoleId = (int)UserRoles.SuperAdmin, 
            Password = "Pass123!" 
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.NOT_HAVE_PERMISSION, ex.Message);
    }

    [Fact]
    public async Task CreateOrUpdateUser_NewUserWithoutPassword_ThrowsAppException()
    {
        var dto = new UserRequestDto 
        { 
            Id = null, 
            Email = "newuser@test.com", 
            FullName = "New User",
            UserName = "newuser",
            RoleId = (int)UserRoles.Player
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.PASSWORD_REQUIRED_FOR_NEW_USER, ex.Message);
    }

    [Fact]
    public async Task CreateUser_AdminCreatedUser_SendsNewUserEmail()
    {
        var dto = new UserRequestDto
        {
            FullName = "Test User",
            Email = "testuser@example.com",
            UserName = "testuser",
            Password = "Test@123",
            RoleId = (int)UserRoles.Player,
            IsRegister = false
        };

        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
            .ReturnsAsync($"Email successfully sent to {dto.Email}");

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
            UserName = "testuser1",
            Password = "Test@123",
            RoleId = (int)UserRoles.Player,
            IsRegister = true
        };

        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
            .ReturnsAsync(string.Format(Constants.EMAIL_SENT_SUCCESS, dto.Email));

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
            RoleId = (int)UserRoles.Player,
            ProfilePic = fileMock.Object,
            IsRegister = false
        };

        _commonServiceMock
            .Setup(s => s.SaveFile(dto.ProfilePic, "users"))
            .ReturnsAsync("uploads/users/test.jpg");

        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
            .ReturnsAsync("Email successfully sent to imageuser@example.com");

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
            Password = "1234",
            RoleId = (int)UserRoles.Player
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
            Password = "1234",
            RoleId = (int)UserRoles.Player
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Equal(Constants.DUPLICATE_USERNAME, ex.Message);
    }

    [Fact]
    public async Task CreateUser_RegisterEmailFails_ThrowsException()
    {
        var dto = new UserRequestDto
        {
            FullName = "Test User",
            Email = "failuser1@example.com",
            UserName = "failuser",
            Password = "Test@123",
            RoleId = (int)UserRoles.Player,
            IsRegister = true
        };

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
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("newpic.jpg");
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[100]));

        var dto = new UserRequestDto
        {
            Id = 3, // Charlie
            FullName = "Updated Name",
            Email = "charlie@example.com",
            UserName = "charlie",
            RoleId = (int)UserRoles.Player,
            ProfilePic = fileMock.Object,
            Bio = "Updated bio"
        };

        _commonServiceMock
            .Setup(s => s.SaveFile(dto.ProfilePic, "users"))
            .ReturnsAsync("users/newpic.jpg");

        _commonServiceMock
            .Setup(s => s.DeleteFile(It.IsAny<string>()))
            .Verifiable();

        _sqlQueryRepositoryMock
            .Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(), 
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = true,
                Message = "User updated successfully."
            });

        var (Success, Message) = await _userService.CreateOrUpdateUser(dto);

        Assert.True(Success);
        _commonServiceMock.Verify(s => s.SaveFile(dto.ProfilePic, "users"), Times.Once);
        _commonServiceMock.Verify(s => s.DeleteFile("old-profile.jpg"), Times.Once);
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
            RoleId = (int)UserRoles.Player
        };

        _sqlQueryRepositoryMock
            .Setup(s => s.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(new CreateUpdateResponseDto
            {
                Success = false,
                Message = string.Format(Constants.USER_NOT_FOUND, 99)
            });

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.CreateOrUpdateUser(dto));
        Assert.Contains("not found", ex.Message.ToLower());
    }

    [Fact]
    public async Task UpdateUser_DoesNotSendEmail()
    {
        var dto = new UserRequestDto
        {
            Id = 2, // Bob
            FullName = "Updated Name",
            Email = "bob@example.com",
            UserName = "bob",
            RoleId = (int)UserRoles.Player,
            IsRegister = false
        };

        _sqlQueryRepositoryMock
            .Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(
                It.IsAny<string>(), 
                It.IsAny<NpgsqlParameter[]>()))
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
        _context.ChangeTracker.Clear();

        var actionRequest = new UserActionRequest
        {
            Id = 2, // Bob (not current user)
            Action = UserActionType.Delete
        };

        var message = await _userService.UpdateUserByAction(actionRequest);

        Assert.Contains("deleted", message.ToLower());

        // Verify user is marked as deleted
        var user = await _context.Users.FindAsync(2);
        Assert.NotNull(user);
        Assert.True(user.IsDeleted);
    }

    [Fact]
    public async Task UpdateUserByAction_CannotDeleteSelf_ThrowsException()
    {
        var actionRequest = new UserActionRequest
        {
            Id = 1, // Current user (Alice)
            Action = UserActionType.Delete
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.UpdateUserByAction(actionRequest));
        Assert.Equal(Constants.CANNOT_MODIFY_SELF, ex.Message);
    }

    [Fact]
    public async Task UpdateUserByAction_ChangeStatus_Success()
    {
        _context.ChangeTracker.Clear();

        var actionRequest = new UserActionRequest
        {
            Id = 2, // Bob
            Action = UserActionType.ChangeStatus,
            NewStatus = UserStatus.Active
        };

        var message = await _userService.UpdateUserByAction(actionRequest);

        Assert.Contains("changed", message.ToLower());

        // Verify status changed
        var user = await _context.Users.FindAsync(2);
        Assert.Equal((int)UserStatus.Active, user.Status);
    }

    [Fact]
    public async Task UpdateUserByAction_StatusAlreadySet_ThrowsAppException()
    {
        _context.ChangeTracker.Clear();

        var request = new UserActionRequest
        {
            Id = 2, // Bob is already Suspended
            Action = UserActionType.ChangeStatus,
            NewStatus = UserStatus.Suspended
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.UpdateUserByAction(request));
        Assert.Contains("already", ex.Message.ToLower());
    }

    [Fact]
    public async Task UpdateUserByAction_NullStatus_ThrowsAppException()
    {
        var request = new UserActionRequest
        {
            Id = 2,
            Action = UserActionType.ChangeStatus,
            NewStatus = null
        };

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.UpdateUserByAction(request));
        Assert.Equal(Constants.STATUS_REQUIRED, ex.Message);
    }

    [Fact]
    public async Task UpdateUserByAction_ChangeStatus_ToSuspended_SendsSuspensionEmail()
    {
        _context.ChangeTracker.Clear();

        var actionRequest = new UserActionRequest
        {
            Id = 3, // Charlie (Active)
            Action = UserActionType.ChangeStatus,
            NewStatus = UserStatus.Suspended
        };

        _commonServiceMock
            .Setup(s => s.SendEmailFromTemplate(It.Is<TemplatedEmailRequestDto>(dto =>
                dto.TemplateType == EmailTemplateType.AccountSuspension &&
                dto.ToEmail == "charlie@example.com")))
            .ReturnsAsync("Email Sent")
            .Verifiable();

        var result = await _userService.UpdateUserByAction(actionRequest);

        Assert.Contains("changed", result.ToLower());
        _commonServiceMock.Verify();
    }

    #endregion

    #region ExportUserData

    [Fact]
    public async Task UserExportData_WithValidData_ReturnsExcelStream()
    {
        var query = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10
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

        var result = await _userService.UserExportData(query);

        Assert.NotNull(result);
        Assert.Equal(stream, result);
    }

    [Fact]
    public async Task UserExportData_ShouldAddRowNumbers()
    {
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

        var result = await _userService.UserExportData(query);

        Assert.NotEmpty(capturedList);
        Assert.Equal(1, capturedList[0].No);
    }

    [Fact]
    public async Task UserExportData_WhenNoData_ThrowsAppException()
    {
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();

        var query = new PageListRequest();

        var ex = await Assert.ThrowsAsync<AppException>(() => _userService.UserExportData(query));
        Assert.Equal(Constants.USER_DATA_NULL, ex.Message);
    }

    [Fact]
    public async Task UserExportData_WithSearchAndFilters_SetsWorksheetMetadata()
    {
        var query = new PageListRequest
        {
            SearchTerm = "bob",
            Filters = new FilterDto
            {
                Role = UserRoles.Player,
                Status = UserStatus.Suspended
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
                    Assert.Equal("Bob Smith", list[0].FullName);
                })
            .Returns(new MemoryStream());

        var result = await _userService.UserExportData(query);

        Assert.NotNull(result);
        Assert.NotNull(capturedSetup);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("TestSheet");

        capturedSetup.Invoke(worksheet);

        Assert.Equal("Search Text:", worksheet.Cell("A7").Value);
        Assert.Equal("bob", worksheet.Cell("B7").Value);

        Assert.Equal("Total Records:", worksheet.Cell("D7").Value);
        Assert.Equal("1", worksheet.Cell("E7").Value.ToString());

        Assert.Equal("Filter:", worksheet.Cell("G7").Value);
        var filterText = worksheet.Cell("H7").Value.ToString();
        Assert.Contains("Role: Player", filterText);
        Assert.Contains("Status: Suspended", filterText);
    }

    #endregion
}