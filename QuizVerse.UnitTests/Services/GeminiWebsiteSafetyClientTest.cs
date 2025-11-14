using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Enums;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class GeminiWebsiteSafetyClientTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IGeminiModelService> _modelRotatorMock;
        private readonly Mock<IAiLogService> _aiLogServiceMock;

        public GeminiWebsiteSafetyClientTests()
        {
            _configMock = new Mock<IConfiguration>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _modelRotatorMock = new Mock<IGeminiModelService>();
            _aiLogServiceMock = new Mock<IAiLogService>();

            // Setup configuration
            _configMock.Setup(c => c["ApiKeys:GeminiApiKey"]).Returns("FAKE-KEY");

            // Default model
            _modelRotatorMock.Setup(m => m.GetNextAvailableModelAsync())
                .ReturnsAsync(AiModelName.Gemini2Point5FlashLite);

            // Log service setup
            _aiLogServiceMock.Setup(a => a.StartApiCall(It.IsAny<AiApiCallStartDetail>()))
                .Returns(new AiProcessLog { Id = 1, StartTime = DateTime.UtcNow });
            _aiLogServiceMock.Setup(a => a.EndApiCall(It.IsAny<AiProcessLog>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
        }

        private static HttpClient FakeHttp(HttpStatusCode statusCode, string content)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });

            return new HttpClient(handlerMock.Object);
        }

        [Fact]
        public async Task IsUnsafeAsync_ShouldReturnSafe_WhenResponseContainsSafeMessage()
        {
            // Arrange
            var safeText = Constants.WEB_SAFE_MESSAGE_GEMINI + " verified safe";
            var json = $@"{{ ""candidates"": [ {{ ""content"": {{ ""parts"": [ {{ ""text"": ""{safeText}"" }} ] }} }} ] }}";

            var http = FakeHttp(HttpStatusCode.OK, json);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

            var service = new GeminiWebsiteSafetyClient(
                _configMock.Object,
                _httpClientFactoryMock.Object,
                _modelRotatorMock.Object,
                _aiLogServiceMock.Object
            );

            // Act
            var (isUnsafe, message) = await service.IsUnsafeAsync("https://safe.example");

            // Assert
            Assert.False(isUnsafe);
            Assert.Contains(Constants.WEB_SAFE_MESSAGE_GEMINI, message, StringComparison.OrdinalIgnoreCase);
            _aiLogServiceMock.Verify(a => a.EndApiCall(It.IsAny<AiProcessLog>(), true), Times.Once);
            _modelRotatorMock.Verify(m => m.RecordRequestAsync(It.IsAny<AiModelName>()), Times.Once);
        }

        [Fact]
        public async Task IsUnsafeAsync_ShouldReturnUnsafe_WhenResponseDoesNotContainSafeMessage()
        {
            // Arrange
            var unsafeText = "Malicious phishing content";
            var json = $@"{{ ""candidates"": [ {{ ""content"": {{ ""parts"": [ {{ ""text"": ""{unsafeText}"" }} ] }} }} ] }}";

            var http = FakeHttp(HttpStatusCode.OK, json);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

            var service = new GeminiWebsiteSafetyClient(
                _configMock.Object,
                _httpClientFactoryMock.Object,
                _modelRotatorMock.Object,
                _aiLogServiceMock.Object
            );

            // Act
            var (isUnsafe, message) = await service.IsUnsafeAsync("https://unsafe.example");

            // Assert
            Assert.True(isUnsafe);
            Assert.Contains("phishing", message, StringComparison.OrdinalIgnoreCase);
            _modelRotatorMock.Verify(m => m.RecordRequestAsync(It.IsAny<AiModelName>()), Times.Once);
        }

        [Fact]
        public async Task IsUnsafeAsync_ShouldThrow503_WhenResponseEmpty()
        {
            // Arrange
            var json = @"{ ""candidates"": [ { ""content"": { ""parts"": [] } } ] }";
            var http = FakeHttp(HttpStatusCode.OK, json);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

            var service = new GeminiWebsiteSafetyClient(
                _configMock.Object,
                _httpClientFactoryMock.Object,
                _modelRotatorMock.Object,
                _aiLogServiceMock.Object
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => service.IsUnsafeAsync("https://empty.example"));
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, ex.StatusCode);
        }

        [Fact]
        public async Task IsUnsafeAsync_ShouldThrow503_WhenHttpRequestFails()
        {
            // Arrange
            var http = FakeHttp(HttpStatusCode.BadGateway, "error");
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

            var service = new GeminiWebsiteSafetyClient(
                _configMock.Object,
                _httpClientFactoryMock.Object,
                _modelRotatorMock.Object,
                _aiLogServiceMock.Object
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => service.IsUnsafeAsync("https://fail.example"));
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, ex.StatusCode);

            _aiLogServiceMock.Verify(a => a.EndApiCall(It.IsAny<AiProcessLog>(), false), Times.Once);
        }

        [Fact]
        public async Task IsUnsafeAsync_ShouldThrow500_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("unexpected"));

            var http = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

            var service = new GeminiWebsiteSafetyClient(
                _configMock.Object,
                _httpClientFactoryMock.Object,
                _modelRotatorMock.Object,
                _aiLogServiceMock.Object
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => service.IsUnsafeAsync("https://crash.example"));
            Assert.Equal(StatusCodes.Status500InternalServerError, ex.StatusCode);
        }

        [Fact]
        public async Task IsUnsafeAsync_ShouldCall_StartAndEndApiCall()
        {
            // Arrange
            var safeText = Constants.WEB_SAFE_MESSAGE_GEMINI;
            var json = $@"{{ ""candidates"": [ {{ ""content"": {{ ""parts"": [ {{ ""text"": ""{safeText}"" }} ] }} }} ] }}";

            var http = FakeHttp(HttpStatusCode.OK, json);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

            var service = new GeminiWebsiteSafetyClient(
                _configMock.Object,
                _httpClientFactoryMock.Object,
                _modelRotatorMock.Object,
                _aiLogServiceMock.Object
            );

            // Act
            await service.IsUnsafeAsync("https://log.example");

            // Assert
            _aiLogServiceMock.Verify(a => a.StartApiCall(It.IsAny<AiApiCallStartDetail>()), Times.Once);
            _aiLogServiceMock.Verify(a => a.EndApiCall(It.IsAny<AiProcessLog>(), true), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenApiKeyMissing()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["ApiKeys:GeminiApiKey"]).Returns((string)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GeminiWebsiteSafetyClient(
                    config.Object,
                    _httpClientFactoryMock.Object,
                    _modelRotatorMock.Object,
                    _aiLogServiceMock.Object));
        }
    }
}
