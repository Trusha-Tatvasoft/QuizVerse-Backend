using System.Net;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class FetchContentFromUrlServiceTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IGeminiWebsiteSafetyClient> _geminiMock;
    private readonly FetchContentFromUrlService _service;

    public FetchContentFromUrlServiceTest()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _geminiMock = new Mock<IGeminiWebsiteSafetyClient>();

        var dummyHttpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(dummyHttpClient);

        _service = new FetchContentFromUrlService(
            _httpClientFactoryMock.Object,
            _geminiMock.Object
        );
    }

    private static HttpClient FakeHttp(HttpStatusCode status, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(content)
            });

        return new HttpClient(handlerMock.Object);
    }

    [Theory]
    [InlineData("notaurl")]
    [InlineData("ftp://example.com")]
    public async Task FetchAndValidateAsync_ShouldThrow400_WhenInvalidUrl(string url)
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => _service.FetchAndValidateAsync(url));
        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        Assert.Contains(Constants.INVALID_URL_PROVIDED, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAndValidateAsync_ShouldThrow403_WhenGeminiMarksUnsafe()
    {
        var http = FakeHttp(HttpStatusCode.OK, "<html><body>Unsafe content</body></html>");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

        _geminiMock.Setup(g => g.IsUnsafeAsync(It.IsAny<string>()))
            .ReturnsAsync((true, "Adult content"));

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.FetchAndValidateAsync("https://unsafe.example"));
        Assert.Equal(StatusCodes.Status500InternalServerError, ex.StatusCode);
        Assert.Contains("Adult", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAndValidateAsync_ShouldThrow403_WhenHttpRequestFails()
    {
        var http = FakeHttp(HttpStatusCode.Forbidden, "error");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

        _geminiMock.Setup(g => g.IsUnsafeAsync(It.IsAny<string>()))
            .ReturnsAsync((false, Constants.WEB_SAFE_MESSAGE_GEMINI));

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.FetchAndValidateAsync("https://fail.example"));
        Assert.Equal(StatusCodes.Status500InternalServerError, ex.StatusCode);
    }

    [Fact]
    public async Task FetchAndValidateAsync_ShouldReturn_CleanedContent_WhenValid()
    {
        string html = @"<!DOCTYPE html>
                        <html>
                        <head>
                        <title>Test</title>
                        <script>evil()</script>
                        <style>body{}</style>
                        </head>
                        <body>
                        <!-- comment -->
                        <h1>Hello &amp; welcome!</h1>
                        <p>Visit us at https://example.com or email contact@example.com</p>
                        </body>
                        </html>";

        var http = FakeHttp(HttpStatusCode.OK, html);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);
        _geminiMock.Setup(g => g.IsUnsafeAsync(It.IsAny<string>())).ReturnsAsync((false, Constants.WEB_SAFE_MESSAGE_GEMINI));

        // recreate service after httpClientFactory setup
        var service = new FetchContentFromUrlService(_httpClientFactoryMock.Object, _geminiMock.Object);

        var result = await service.FetchAndValidateAsync("https://ok.example");

        Assert.Contains("Hello", result);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("comment", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://example.com", result);
        Assert.DoesNotContain("contact@example.com", result);
    }

    [Fact]
    public async Task FetchAndValidateAsync_ShouldThrow500_WhenGeminiThrows()
    {
        var http = FakeHttp(HttpStatusCode.OK, "<html>text</html>");
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);
        _geminiMock.Setup(g => g.IsUnsafeAsync(It.IsAny<string>())).ThrowsAsync(new Exception("boom"));

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.FetchAndValidateAsync("https://err.example"));
        Assert.Equal(StatusCodes.Status500InternalServerError, ex.StatusCode);
    }

    [Fact]
    public async Task FetchAndValidateAsync_ShouldCatch_HttpRequestException_As403()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        var http = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(http);

        _geminiMock.Setup(g => g.IsUnsafeAsync(It.IsAny<string>())).ReturnsAsync((false, Constants.WEB_SAFE_MESSAGE_GEMINI));

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.FetchAndValidateAsync("https://networkfail.example"));
        Assert.Equal(StatusCodes.Status500InternalServerError, ex.StatusCode);
    }
}

