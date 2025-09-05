using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Expressions;
using System.Security.Claims;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class EmailTemplatesServiceTests
    {
        private readonly Mock<IGenericRepository<EmailTemplete>> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly EmailTemplatesService _service;

        public EmailTemplatesServiceTests()
        {
            _repoMock = new Mock<IGenericRepository<EmailTemplete>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // Setup real ClaimsPrincipal with UserId
            var claims = new List<Claim> { new Claim(ClaimTypes.UserData, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var context = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            _service = new EmailTemplatesService(
                _repoMock.Object,
                _mapperMock.Object,
                _httpContextAccessorMock.Object
            );
        }

        [Fact]
        public void GetAllEmailTemplates_Should_OrderById_When_NoSortColumn()
        {
            // Arrange
            var templates = new List<EmailTemplete>
        {
            new EmailTemplete { Id = 2, Title = "B" },
            new EmailTemplete { Id = 1, Title = "A" }
        }.AsQueryable();

            _repoMock.Setup(r => r.GetQueryableInclude()).Returns(templates);

            _mapperMock.Setup(m => m.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()))
                .Returns((List<EmailTemplete> list) => list.Select(x => new EmailTemplatesResponseDto { Id = x.Id, Title = x.Title }).ToList());

            var request = new PageListRequest(); // no SortColumn

            // Act
            var result = _service.GetAllEmailTemplates(request);

            // Assert
            Assert.Equal(2, result.TotalRecords);
            Assert.Equal(new[] { 1, 2 }, result.Records.Select(r => r.Id));
        }

        [Fact]
        public void GetAllEmailTemplates_Should_OrderBy_Title_Desc()
        {
            // Arrange
            var templates = new List<EmailTemplete>
        {
            new EmailTemplete { Id = 1, Title = "A" },
            new EmailTemplete { Id = 2, Title = "C" },
            new EmailTemplete { Id = 3, Title = "B" }
        }.AsQueryable();

            _repoMock.Setup(r => r.GetQueryableInclude()).Returns(templates);

            _mapperMock.Setup(m => m.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()))
                .Returns((List<EmailTemplete> list) => list.Select(x => new EmailTemplatesResponseDto { Id = x.Id, Title = x.Title }).ToList());

            var request = new PageListRequest
            {
                SortColumn = "Title",
                SortDescending = true
            };

            // Act
            var result = _service.GetAllEmailTemplates(request);

            // Assert
            Assert.Equal(new[] { "C", "B", "A" }, result.Records.Select(r => r.Title));
        }

        [Fact]
        public void GetAllEmailTemplates_Should_Invert_Boolean_Sort()
        {
            // Arrange
            var templates = new List<EmailTemplete>
        {
            new EmailTemplete { Id = 1, Status = true },
            new EmailTemplete { Id = 2, Status = false }
        }.AsQueryable();

            _repoMock.Setup(r => r.GetQueryableInclude()).Returns(templates);

            _mapperMock.Setup(m => m.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()))
                .Returns((List<EmailTemplete> list) => list.Select(x => new EmailTemplatesResponseDto { Id = x.Id, Status = x.Status }).ToList());

            var request = new PageListRequest
            {
                SortColumn = nameof(EmailTemplete.Status),
                SortDescending = false // will be inverted in service
            };

            // Act
            var result = _service.GetAllEmailTemplates(request);

            // Assert
            Assert.Equal(new[] { true, false }, result.Records.Select(r => r.Status));
        }

        [Fact]
        public void GetAllEmailTemplates_Should_Throw_For_Invalid_SortColumn()
        {
            // Arrange
            var templates = new List<EmailTemplete>
        {
            new EmailTemplete { Id = 1, Title = "Test" }
        }.AsQueryable();

            _repoMock.Setup(r => r.GetQueryableInclude()).Returns(templates);

            var request = new PageListRequest { SortColumn = "DoesNotExist" };

            // Act & Assert
            var ex = Assert.Throws<AppException>(() => _service.GetAllEmailTemplates(request));
            Assert.Contains("DoesNotExist", ex.Message);
        }

        [Fact]
        public async Task AddOrEditEmailTemplate_Should_Add_When_New()
        {
            // Arrange
            var dto = new EmailTemplatesRequestDTO
            {
                Id = 0,
                TemplateType = (int)EmailTemplateType.AccountSuspension,
                Title = "Welcome",
                Subject = "Hello",
                Body = "Hi {{user}}{{email}}",
                Status = true
            };

            _repoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<EmailTemplete, bool>>>())).ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<EmailTemplete>(dto)).Returns(new EmailTemplete { Id = 1 });

            // Act
            var result = await _service.AddOrEditEmailTemplate(dto);

            // Assert
            Assert.Equal(Constants.EMAIL_TEMPLATE_ADDED, result);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<EmailTemplete>()), Times.Once);
        }

        [Fact]
        public async Task AddOrEditEmailTemplate_Should_Throw_When_AlreadyExists()
        {
            var dto = new EmailTemplatesRequestDTO
            {
                Id = 0,
                TemplateType = (int)EmailTemplateType.AccountSuspension,
                Title = "Duplicate",
                Subject = "Hello",
                Body = "Hi {{user}}{{email}}"
            };

            _repoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<EmailTemplete, bool>>>())).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<AppException>(() => _service.AddOrEditEmailTemplate(dto));
            Assert.Equal(Constants.EMAIL_TEMPLATE_ALREDY_AVAILABLE_FOR_SAME_TYPE, ex.Message);
        }

        [Fact]
        public async Task AddOrEditEmailTemplate_Should_Update_When_Existing()
        {
            // Arrange
            var dto = new EmailTemplatesRequestDTO
            {
                Id = 1,
                TemplateType = (int)EmailTemplateType.AccountSuspension,
                Title = "Updated",
                Subject = "Hi",
                Body = "Hi {{user}}{{email}}",
                Status = true
            };

            var existing = new EmailTemplete
            {
                Id = 1,
                TemplateType = (int)EmailTemplateType.WelComeEmail,
                IsDeleted = false,
                Status = false
            };

            _repoMock.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
                It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()
            )).ReturnsAsync(existing);

            _mapperMock.Setup(m => m.Map(It.IsAny<EmailTemplatesRequestDTO>(), It.IsAny<EmailTemplete>()))
                .Returns((EmailTemplatesRequestDTO src, EmailTemplete dest) =>
                {
                    dest.TemplateType = src.TemplateType;
                    dest.Title = src.Title;
                    dest.Subject = src.Subject;
                    dest.Body = src.Body;
                    dest.Status = (bool)src.Status;
                    return dest;
                });

            // Act
            var result = await _service.AddOrEditEmailTemplate(dto);

            // Assert
            Assert.Equal(Constants.EMAIL_TEMPLATE_UPDATED, result);
            Assert.True(existing.Status); // updated
            Assert.Equal(dto.Title, existing.Title); // updated field
            _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task GetEmailTemplateById_Should_Return_Template()
        {
            // Arrange
            var entity = new EmailTemplete { Id = 5, Title = "Test" };
            var dto = new EmailTemplatesResponseDto { Id = 5, Title = "Test" };

            _repoMock.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
                It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()
            )).ReturnsAsync(entity);

            _mapperMock.Setup(m => m.Map<EmailTemplatesResponseDto>(entity)).Returns(dto);

            // Act
            var result = await _service.GetEmailTemplateById(5);

            // Assert
            Assert.Equal(5, result.Id);
            Assert.Equal("Test", result.Title);
        }

        [Fact]
        public async Task UpdateEmailTemplateByAction_Should_Delete()
        {
            var entity = new EmailTemplete { Id = 10, IsDeleted = false };
            _repoMock
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
                    It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()
                ))
                .ReturnsAsync(entity);

            var dto = new EmailTemplateActionRequestDTO { Id = 10, Action = EmailTemplateActionType.Delete };

            var result = await _service.UpdateEmailTemplateByAction(dto);

            Assert.Equal(Constants.EMAIL_TEMPLATE_DELETE, result);
            Assert.True(entity.IsDeleted);
            _repoMock.Verify(r => r.UpdateAsync(entity), Times.Once);
        }

        [Fact]
        public async Task UpdateEmailTemplateByAction_Should_ChangeStatus()
        {
            var entity = new EmailTemplete { Id = 11, Status = false };
            _repoMock
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
                    It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()
                ))
                .ReturnsAsync(entity);

            var dto = new EmailTemplateActionRequestDTO { Id = 11, Action = EmailTemplateActionType.ChangeStatus };

            var result = await _service.UpdateEmailTemplateByAction(dto);

            Assert.Equal(Constants.EMAIL_TEMPLATE_STATUS_UPDATED, result);
            Assert.True(entity.Status);
            _repoMock.Verify(r => r.UpdateAsync(entity), Times.Once);
        }
    }
}
