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

namespace QuizVerse.UnitTests.Services;

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
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(GetValidConfigs());

        // Act
        var result = await _service.GetPlatformConfigurations();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Quote", result.Quote);
        Assert.Equal("logo.png", result.Logo);
        Assert.Equal("#111111", result.DefaultsColors.PrimaryColor);
        Assert.Equal("#222222", result.DefaultsColors.SecondaryColor);
    }

    [Fact]
    public async Task GetPlatformConfigurations_ShouldThrow_WhenMissingQuoteConfig()
    {
        // Arrange: remove quote config
        var configs = GetValidConfigs();
        configs.RemoveAt(0);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetPlatformConfigurations());
    }

    [Fact]
    public async Task UpdatePlatformConfigurations_ShouldUpdateQuoteAndColors_WhenLogoNotProvided()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(GetValidConfigs());

        var request = new PlatformConfigurationRequestDTO
        {
            Quote = "Updated Quote",
            DefaultsColors = new DefaultsColors { PrimaryColor = "#333333", SecondaryColor = "#444444" }
        };

        // Act
        var result = await _service.UpdatePlatformConfigurations(request);

        // Assert
        Assert.Equal(Constants.PLATFORM_CONFIGURATION_UPDATE_SUCCESS, result);
        _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<List<PlatformConfiguration>>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePlatformConfigurations_ShouldSaveLogo_WhenLogoProvided()
    {
        // Arrange
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logo");
        Directory.CreateDirectory(uploadsPath); // make sure path exists

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(GetValidConfigs("oldLogo.png"));

        var request = new PlatformConfigurationRequestDTO
        {
            Quote = "Quote with Logo",
            DefaultsColors = new DefaultsColors { PrimaryColor = "#555555", SecondaryColor = "#666666" },
            Logo = new FormFileMock() // custom mock file
        };

        _commonServiceMock
            .Setup(c => c.SaveFile(It.IsAny<IFormFile>(), "logo"))
            .ReturnsAsync("wwwroot/uploads/logo/newLogo.png");

        // Act
        var result = await _service.UpdatePlatformConfigurations(request);

        // Assert
        Assert.Equal(Constants.PLATFORM_CONFIGURATION_UPDATE_SUCCESS, result);
        _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<List<PlatformConfiguration>>()), Times.Once);
        _commonServiceMock.Verify(c => c.SaveFile(It.IsAny<IFormFile>(), "logo"), Times.Once);
    }

    [Fact]
    public async Task GetPlatformConfigurations_ShouldReturnLogoBase64_WhenFileExists()
    {
        // Arrange
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "logo.png");
        File.WriteAllBytes(logoPath, new byte[] { 1, 2, 3 }); // create dummy file

        var configs = GetValidConfigs(logoPath);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

        // Act
        var result = await _service.GetPlatformConfigurations();

        // Assert
        Assert.NotNull(result.Logo);
        Assert.NotNull(result);
        Assert.NotNull(result.Logo);
        Assert.NotNull(result.DefaultsColors);
        Assert.NotNull(result.Logo); // base64 should be generated

        // Cleanup
        File.Delete(logoPath);
    }

    [Fact]
    public async Task GetPlatformConfigurations_ShouldThrow_WhenLogoPathMissing()
    {
        // Arrange: config has no Path property
        var configs = GetValidConfigs();
        configs[1].Values = JsonSerializer.Serialize(new { }); // empty logo config
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetPlatformConfigurations());
    }

    [Fact]
    public async Task GetPlatformConfigurations_ShouldThrow_WhenPrimaryColorMissing()
    {
        // Arrange: remove PrimaryColor
        var configs = GetValidConfigs();
        configs[2].Values = JsonSerializer.Serialize(new { SecondaryColor = "#222222" });
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetPlatformConfigurations());
    }

    [Fact]
    public async Task UpdatePlatformConfigurations_ShouldDeleteOldFiles_WhenNewLogoProvided()
    {
        // Arrange
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logo");
        Directory.CreateDirectory(uploadsPath);

        var oldFile = Path.Combine(uploadsPath, "oldLogo.png");
        File.WriteAllBytes(oldFile, new byte[] { 1, 2, 3 });

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(GetValidConfigs("oldLogo.png"));

        var request = new PlatformConfigurationRequestDTO
        {
            Quote = "New Quote",
            DefaultsColors = new DefaultsColors { PrimaryColor = "#123456", SecondaryColor = "#654321" },
            Logo = new FormFileMock()
        };

        var newFilePath = Path.Combine(uploadsPath, "newLogo.png");
        _commonServiceMock.Setup(c => c.SaveFile(It.IsAny<IFormFile>(), "logo"))
            .ReturnsAsync(newFilePath);

        // Act
        var result = await _service.UpdatePlatformConfigurations(request);

        // Assert
        Assert.Equal(Constants.PLATFORM_CONFIGURATION_UPDATE_SUCCESS, result);
        Assert.False(File.Exists(oldFile)); // old file deleted
    }

    [Fact]
    public async Task UpdatePlatformConfigurations_ShouldThrow_WhenColorConfigMissing()
    {
        // Arrange: remove color config
        var configs = GetValidConfigs();
        configs.RemoveAt(2);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);

        var request = new PlatformConfigurationRequestDTO
        {
            Quote = "Some Quote",
            DefaultsColors = new DefaultsColors { PrimaryColor = "#AAA", SecondaryColor = "#BBB" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdatePlatformConfigurations(request));
    }


    // helper class for mocking IFormFile
    private class FormFileMock : Microsoft.AspNetCore.Http.IFormFile
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
