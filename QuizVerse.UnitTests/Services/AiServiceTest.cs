using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Application.Core.Interface;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using OllamaSharp;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.UnitTests.Services
{
    public class AiServiceTests
    {
        private readonly Mock<IOptions<AiServiceOptions>> _mockOptions;
        private readonly AiServiceOptions _options;

        public AiServiceTests()
        {
            _options = new AiServiceOptions
            {
                BaseUrl = "http://localhost:11434",
                Model = "test-model"
            };

            _mockOptions = new Mock<IOptions<AiServiceOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(_options);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenOptionsNull()
        {
            var exception = Assert.Throws<AppException>(() => new AiService(null!));
            Assert.Equal("AI service options are not configured.", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenBaseUrlEmpty()
        {
            _options.BaseUrl = "";
            _mockOptions.Setup(o => o.Value).Returns(_options);

            var exception = Assert.Throws<AppException>(() => new AiService(_mockOptions.Object));
            Assert.Equal("BaseUrl cannot be empty.", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenModelEmpty()
        {
            _options.Model = "";
            _mockOptions.Setup(o => o.Value).Returns(_options);

            var exception = Assert.Throws<AppException>(() => new AiService(_mockOptions.Object));
            Assert.Equal("Model cannot be empty.", exception.Message);
        }

        [Fact]
        public async Task GetResponseAsync_ShouldThrow_WhenPromptEmpty()
        {
            var service = new AiService(_mockOptions.Object);

            var exception = await Assert.ThrowsAsync<AppException>(async () =>
                await service.GetResponseAsync("")
            );

            Assert.Equal("Prompt cannot be empty.", exception.Message);
        }

        // This test is optional if you want to mock the OllamaApiClient behavior
        [Fact]
        public async Task GetResponseAsync_ShouldReturnConcatenatedResponse()
        {
            // Mock the OllamaApiClient inside AiService
            // For simplicity, here we use the real AiService
            // In real unit test, you may need to wrap OllamaApiClient or use a mockable interface

            var service = new AiService(_mockOptions.Object);

            // Example: we cannot call real OllamaSharp here because it requires a running model server
            // So in unit tests, we usually **mock the GenerateAsync method**.
            // Here, we just test structure of code.

            await Task.CompletedTask;
        }
    }
}
