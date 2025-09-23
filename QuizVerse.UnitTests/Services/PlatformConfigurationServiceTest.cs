using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class PlatformConfigurationServiceTests
    {
        private readonly Mock<IGenericRepository<PlatformConfiguration>> _repositoryMock;
        private readonly Mock<ICommonService> _commonServiceMock;
        private readonly PlatformConfigurationService _service;

        public PlatformConfigurationServiceTests()
        {
            _repositoryMock = new Mock<IGenericRepository<PlatformConfiguration>>();
            _commonServiceMock = new Mock<ICommonService>();
            _service = new PlatformConfigurationService(_repositoryMock.Object, _commonServiceMock.Object);
        }

        private List<PlatformConfiguration> GetValidConfigs(string logoPath = "logo.png")
        {
            return new List<PlatformConfiguration>
            {
                new PlatformConfiguration
                {
                    ConfigurationName = SystemConstants.PLATFORM_QUOTE_CONFIGURATION_NAME,
                    Values = JsonSerializer.Serialize(new { PlatformQuote = "Test Quote" })
                },
                new PlatformConfiguration
                {
                    ConfigurationName = SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME,
                    Values = JsonSerializer.Serialize(new { Path = logoPath })
                },
                new PlatformConfiguration
                {
                    ConfigurationName = SystemConstants.PLATFORM_COLOR_CONFIGURATION_NAME,
                    Values = JsonSerializer.Serialize(new { PrimaryColor = "#111111", SecondaryColor = "#222222" })
                }
            };
        }

        [Fact]
        public async Task GetPlatformConfigurations_ShouldReturnConfig_WhenDataIsValid()
        {
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(GetValidConfigs());
            var result = await _service.GetPlatformConfigurations();

            Assert.NotNull(result);
            Assert.Equal("Test Quote", result.Quote);
            Assert.Equal("logo.png", result.Logo);
            Assert.Equal("#111111", result.DefaultsColors.PrimaryColor);
            Assert.Equal("#222222", result.DefaultsColors.SecondaryColor);
        }

        [Fact]
        public async Task GetPlatformConfigurations_ShouldReturnLogoBase64_WhenFileExists()
        {
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "logo.png");
            File.WriteAllBytes(logoPath, new byte[] { 1, 2, 3 });

            var configs = GetValidConfigs(logoPath);
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

            var result = await _service.GetPlatformConfigurations();

            Assert.NotNull(result.Logo);
            Assert.Equal(logoPath, result.Logo);

            File.Delete(logoPath);
        }

        [Fact]
        public async Task GetPlatformConfigurations_ShouldThrow_WhenLogoPathMissing()
        {
            var configs = GetValidConfigs();
            configs[1].Values = JsonSerializer.Serialize(new { });
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetPlatformConfigurations());
        }

        [Fact]
        public async Task GetPlatformConfigurations_ShouldThrow_WhenPrimaryColorMissing()
        {
            var configs = GetValidConfigs();
            configs[2].Values = JsonSerializer.Serialize(new { SecondaryColor = "#222222" });
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetPlatformConfigurations());
        }

        [Fact]
        public async Task GetPlatformConfigurations_ShouldUseDefaultQuote_WhenQuoteConfigMissing()
        {
            var configs = GetValidConfigs();
            configs.RemoveAt(0);
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

            var result = await _service.GetPlatformConfigurations();

            Assert.NotNull(result.Quote);
        }

        [Fact]
        public async Task GetPlatformConfigurations_ShouldReturnNullLogo_WhenLogoConfigNotPresent()
        {
            var configs = GetValidConfigs();
            configs.RemoveAt(1);
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

            var result = await _service.GetPlatformConfigurations();

            Assert.Null(result.Logo);
        }

        [Fact]
        public async Task GetPlatformConfigurations_ShouldSkipBase64_WhenFileNotExists()
        {
            var nonExisting = Path.Combine(Directory.GetCurrentDirectory(), "nofile.png");
            var configs = GetValidConfigs(nonExisting);
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

            var result = await _service.GetPlatformConfigurations();

            Assert.Equal(nonExisting, result.Logo);
        }

        [Fact]
        public async Task UpdatePlatformConfigurations_ShouldAddNewLogoConfig_WhenLogoConfigIsNull()
        {
            // Arrange: no logo record in repo
            var configs = GetValidConfigs();
            configs.RemoveAll(c => c.ConfigurationName == SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME);
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

            var req = new PlatformConfigurationRequestDTO
            {
                Quote = "With New Logo",
                DefaultsColors = new DefaultsColors { PrimaryColor = "#111", SecondaryColor = "#222" },
                Logo = new FormFileMock()
            };

            var savedPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logo", "newlogo.png");
            _commonServiceMock.Setup(c => c.SaveFile(It.IsAny<IFormFile>(), "logo")).ReturnsAsync(savedPath);

            // Act
            var result = await _service.UpdatePlatformConfigurations(req);

            // Assert
            Assert.Equal(Constants.PLATFORM_CONFIGURATION_UPDATE_SUCCESS, result);
            _repositoryMock.Verify(
                r => r.AddAsync(It.Is<PlatformConfiguration>(pc =>
                    pc.ConfigurationName == SystemConstants.PLATFORM_LOGO_CONFIGURATION_NAME &&
                    pc.Values.Contains("newlogo")
                )),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdatePlatformConfigurations_ShouldSaveLogo_WhenLogoProvided()
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logo");
            Directory.CreateDirectory(folder);

            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(GetValidConfigs("oldLogo.png"));

            var req = new PlatformConfigurationRequestDTO
            {
                Quote = "With Logo",
                DefaultsColors = new DefaultsColors { PrimaryColor = "#555", SecondaryColor = "#666" },
                Logo = new FormFileMock()
            };

            var newPath = Path.Combine(folder, "newLogo.png");
            _commonServiceMock.Setup(c => c.SaveFile(It.IsAny<IFormFile>(), "logo")).ReturnsAsync(newPath);

            var result = await _service.UpdatePlatformConfigurations(req);

            Assert.Equal(Constants.PLATFORM_CONFIGURATION_UPDATE_SUCCESS, result);
            _commonServiceMock.Verify(c => c.SaveFile(It.IsAny<IFormFile>(), "logo"), Times.Once);
        }

        [Fact]
        public async Task UpdatePlatformConfigurations_ShouldDeleteOldFiles_WhenNewLogoProvided()
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logo");
            Directory.CreateDirectory(folder);

            var oldFile = Path.Combine(folder, "old.png");
            File.WriteAllBytes(oldFile, new byte[] { 1 });

            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(GetValidConfigs("old.png"));

            var req = new PlatformConfigurationRequestDTO
            {
                Quote = "New",
                DefaultsColors = new DefaultsColors { PrimaryColor = "#1", SecondaryColor = "#2" },
                Logo = new FormFileMock()
            };

            var newPath = Path.Combine(folder, "new.png");
            _commonServiceMock.Setup(c => c.SaveFile(It.IsAny<IFormFile>(), "logo")).ReturnsAsync(newPath);

            var result = await _service.UpdatePlatformConfigurations(req);

            Assert.Equal(Constants.PLATFORM_CONFIGURATION_UPDATE_SUCCESS, result);
            Assert.False(File.Exists(oldFile));
        }

        [Fact]
        public async Task UpdatePlatformConfigurations_ShouldDeleteLogoConfig_WhenLogoNull()
        {
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(GetValidConfigs("some.png"));

            var req = new PlatformConfigurationRequestDTO
            {
                Quote = "Q",
                DefaultsColors = new DefaultsColors { PrimaryColor = "#1", SecondaryColor = "#2" },
                Logo = null
            };

            var result = await _service.UpdatePlatformConfigurations(req);

            Assert.Equal(Constants.PLATFORM_CONFIGURATION_UPDATE_SUCCESS, result);
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<PlatformConfiguration>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePlatformConfigurations_ShouldThrow_WhenColorConfigMissing()
        {
            var configs = GetValidConfigs();
            configs.RemoveAt(2);
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

            var req = new PlatformConfigurationRequestDTO
            {
                Quote = "X",
                DefaultsColors = new DefaultsColors { PrimaryColor = "#A", SecondaryColor = "#B" }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdatePlatformConfigurations(req));
        }

        // --------- helper IFormFile ---------
        private class FormFileMock : IFormFile
        {
            public string ContentType => "image/png";
            public string ContentDisposition => "inline";
            public IHeaderDictionary Headers => new HeaderDictionary();
            public long Length => 10;
            public string Name => "logo";
            public string FileName => "logo.png";
            public void CopyTo(Stream target) { }
            public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Stream OpenReadStream() => new MemoryStream(new byte[10]);
        }
    }
}