using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ICustomService> _customServiceMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly UserService _userService;


        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _customServiceMock = new Mock<ICustomService>();
            _configMock = new Mock<IConfiguration>();
            _userService = new UserService(_userRepoMock.Object, _customServiceMock.Object, _configMock.Object);

        }

        [Fact]
        public async Task GetUserById_WhenUserExists_ReturnsUserDto()
        {
            var user = new User
            {
                Id = 1,
                FullName = "Devisha Gajjar",
                Email = "devisha@example.com",
                UserName = "devisha123",
                RoleId = 2,
                Status = (int)UserStatus.Active,
                CreatedDate = DateTime.UtcNow,
                Bio = "Bio",
                ProfilePic = "pic.png"
            };

            _userRepoMock.Setup(r => r.GetAsync(u => u.Id == 1 && !u.IsDeleted)).ReturnsAsync(user);

            var result = await _userService.GetUserById(1);

            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Email, result.Email);
        }

        [Fact]
        public async Task GetUserById_WhenUserNotFound_ThrowsAppException()
        {
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User)null!);

            await Assert.ThrowsAsync<AppException>(() => _userService.GetUserById(100));
        }

        [Fact]
        public async Task CreateUser_WithUniqueEmailAndUsername_CreatesUserAndSendsEmail()
        {
            var dto = new CreateUserDto
            {
                FullName = "Devisha Gajjar",
                Email = "devisha@example.com",
                UserName = "devisha123",
                Password = "Password@123",
                Bio = "test",
                ProfilePic = "pic.png"
            };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User)null!);
            _customServiceMock.Setup(c => c.Hash(dto.Password)).Returns("hashedpwd");

            _configMock.Setup(c => c["EmailSettings:NewUserTemplatePath"]).Returns("TestData/NewUser.html");

            _customServiceMock.Setup(c =>
                        c.SendEmail(dto.Email, "Welcome to QuizVerse!", It.Is<string>(b => b.Contains(dto.Password))))
                        .ReturnsAsync(true);


            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                         .Callback<User>(u => u.Id = 99)
                         .Returns(Task.CompletedTask);

            var result = await _userService.CreateUser(dto);

            Assert.Equal(dto.Email, result.Email);
            Assert.Equal("Email sent successfully.", result.EmailStatus);
        }

        [Fact]
        public async Task CreateUser_WithDuplicateEmail_ThrowsAppException()
        {
            var dto = new CreateUserDto
            {
                Email = "duplicate@example.com",
                UserName = "uniqueuser",
                Password = "pass"
            };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(new User { Email = dto.Email });

            await Assert.ThrowsAsync<AppException>(() => _userService.CreateUser(dto));
        }

        [Fact]
        public async Task CreateUser_WithDuplicateUsername_ThrowsAppException()
        {
            var dto = new CreateUserDto
            {
                Email = "unique@example.com",
                UserName = "duplicateuser",
                Password = "pass"
            };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(new User { UserName = dto.UserName });

            await Assert.ThrowsAsync<AppException>(() => _userService.CreateUser(dto));
        }


        [Fact]
        public async Task UpdateUser_WithValidData_UpdatesSuccessfully()
        {
            var user = new User
            {
                Id = 1,
                Email = "old@example.com",
                UserName = "olduser",
                IsDeleted = false
            };

            var dto = new UpdateUserDto
            {
                FullName = "Updated",
                Email = "new@example.com",
                UserName = "newuser"
            };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user);

            _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _userService.UpdateUser(1, dto);

            Assert.Equal(dto.Email, result.Email);
            Assert.Equal(dto.UserName, result.UserName);
        }

        [Fact]
        public async Task UpdateUser_WithDuplicateEmail_ThrowsAppException()
        {
            var existingUser = new User { Id = 1, Email = "original@example.com", UserName = "originalUser" };
            var conflictingUser = new User { Id = 2, Email = "duplicate@example.com" }; 

            var dto = new UpdateUserDto { Email = "duplicate@example.com", UserName = "newname" };

            _userRepoMock.SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(existingUser)      
                         .ReturnsAsync(conflictingUser);   

            await Assert.ThrowsAsync<AppException>(() => _userService.UpdateUser(1, dto));
        }

        [Fact]
        public async Task UpdateUser_WithDuplicateUsername_ThrowsAppException()
        {
            var existingUser = new User { Id = 1, Email = "original@example.com", UserName = "originalUser" };
            var conflictingUser = new User { Id = 2, UserName = "duplicateUser" }; 

            var dto = new UpdateUserDto { Email = "new@example.com", UserName = "duplicateUser" };

            _userRepoMock.SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(existingUser)
                         .ReturnsAsync(conflictingUser);

            await Assert.ThrowsAsync<AppException>(() => _userService.UpdateUser(1, dto));
        }

        [Fact]
        public async Task UpdateUser_WithProfilePicture_SetsProfilePic()
        {
            var user = new User { Id = 1, Email = "old@example.com", UserName = "oldUser", FullName = "Old User" };

            var dto = new UpdateUserDto
            {
                FullName = "Updated User",
                Email = "updated@example.com",
                UserName = "updatedUser",
                Bio = "New bio",
                ProfilePicture = "profilepic.jpg"
            };

            _userRepoMock.SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user)
                         .ReturnsAsync((User)null!);

            _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _userService.UpdateUser(user.Id, dto);

            Assert.Equal(dto.ProfilePicture, result.ProfilePic);
        }

        [Fact]
        public async Task UpdateUser_WithBio_SetsBio()
        {
            var user = new User { Id = 1, Email = "old@example.com", UserName = "oldUser", FullName = "Old User" };

            var dto = new UpdateUserDto
            {
                FullName = "Updated User",
                Email = "updated@example.com",
                UserName = "updatedUser",
                Bio = "This is a new bio"
            };

            _userRepoMock.SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user)
                         .ReturnsAsync((User)null!);

            _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _userService.UpdateUser(user.Id, dto);

            Assert.Equal(dto.Bio, result.Bio);
        }

        [Fact]
        public async Task DeleteUser_MarksUserAsDeleted()
        {
            var user = new User { Id = 1, IsDeleted = false };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user);

            _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

            await _userService.DeleteUser(1);

            Assert.True(user.IsDeleted);
        }

        [Fact]
        public async Task DeleteUser_WhenUserNotFound_ThrowsAppException()
        {
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync((User)null!);

            await Assert.ThrowsAsync<AppException>(() => _userService.DeleteUser(999));
        }

        [Fact]
        public async Task ChangeUserStatus_WithDifferentStatus_UpdatesStatus()
        {
            var user = new User { Id = 1, Status = (int)UserStatus.Active };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

            await _userService.ChangeUserStatus(1, UserStatus.Suspended);

            Assert.Equal((int)UserStatus.Suspended, user.Status);
        }

        [Fact]
        public async Task ChangeUserStatus_WhenAlreadySet_ThrowsAppException()
        {
            var user = new User { Id = 1, Status = (int)UserStatus.Active };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            await Assert.ThrowsAsync<AppException>(() => _userService.ChangeUserStatus(1, UserStatus.Active));
        }

        [Fact]
        public async Task ChangeUserStatus_WhenUserNotFound_ThrowsAppException()
        {
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync((User)null!);

            await Assert.ThrowsAsync<AppException>(() => _userService.ChangeUserStatus(404, UserStatus.Suspended));
        }

    }
}
