using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class PerspectiveQueueHostedServiceTest
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IGcpApiQueueService> _queueServiceMock;
        private readonly Mock<ILogger<PerspectiveQueueHostedService>> _loggerMock;

        public PerspectiveQueueHostedServiceTest()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _queueServiceMock = new Mock<IGcpApiQueueService>();
            _loggerMock = new Mock<ILogger<PerspectiveQueueHostedService>>();

            // Setup DI scope
            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_scopeFactoryMock.Object);

            _scopeFactoryMock
                .Setup(f => f.CreateScope())
                .Returns(_scopeMock.Object);

            _scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(_serviceProviderMock.Object);

            // When the hosted service asks for the queue service, give it our mock
            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IGcpApiQueueService)))
                .Returns(_queueServiceMock.Object);
        }

        [Fact(DisplayName = "Should call StartProcessingAsync when service runs")]
        public async Task ExecuteAsync_ShouldCallQueueService()
        {
            var hostedService = new PerspectiveQueueHostedService(_serviceProviderMock.Object, _loggerMock.Object, TimeSpan.Zero);

            _queueServiceMock
                .Setup(q => q.StartProcessingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await hostedService.StartAsync(CancellationToken.None);
            await Task.Delay(100); // give background task time to execute

            _queueServiceMock.Verify(q => q.StartProcessingAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(DisplayName = "Should log error if unexpected exception occurs")]
        public async Task ExecuteAsync_ShouldLogErrorOnException()
        {
            var hostedService = new PerspectiveQueueHostedService(_serviceProviderMock.Object, _loggerMock.Object, TimeSpan.Zero);
            _queueServiceMock
                .Setup(q => q.StartProcessingAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test failure"));

            await hostedService.StartAsync(CancellationToken.None);
            await Task.Delay(100);

            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fatal error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact(DisplayName = "Should handle OperationCanceledException gracefully")]
        public async Task ExecuteAsync_ShouldHandleCancellationGracefully()
        {
            // Arrange
            var hostedService = new PerspectiveQueueHostedService(_serviceProviderMock.Object, _loggerMock.Object);

            _queueServiceMock
                .Setup(q => q.StartProcessingAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var ex = await Record.ExceptionAsync(() => hostedService.StartAsync(CancellationToken.None));

            // Assert
            Assert.Null(ex); // No exception should propagate
        }

        [Fact(DisplayName = "Should stop gracefully when StopAsync is called")]
        public async Task StopAsync_ShouldLogAndStop()
        {
            // Arrange
            var hostedService = new PerspectiveQueueHostedService(_serviceProviderMock.Object, _loggerMock.Object);

            // Act
            await hostedService.StopAsync(CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("stopping")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }
    }
}
