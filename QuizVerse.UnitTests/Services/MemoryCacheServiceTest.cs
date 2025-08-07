using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using QuizVerse.Application.Core.Service;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class MemoryCacheServiceTest
    {
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly MemoryCacheService _service;

        // Delegate to handle the TryGetValue callback
        private delegate void TryGetValueCallback(object key, out object value);

        public MemoryCacheServiceTest()
        {
            _memoryCacheMock = new Mock<IMemoryCache>();
            _configurationMock = new Mock<IConfiguration>();

            // Default configuration setup
            _configurationMock.Setup(c => c["MemoryCacheSetting:MemoryCacheExpiryHours"])
                .Returns("1");

            _service = new MemoryCacheService(
                _memoryCacheMock.Object,
                _configurationMock.Object);
        }

        [Fact]
        public void GetOrSet_ReturnsCachedValue_WhenExists()
        {
            // Arrange
            var key = "testKey";
            var expectedValue = "cachedValue";

            // Create a wrapper to handle the out parameter
            object? cachedValue = expectedValue;

            // Setup TryGetValue behavior
            _memoryCacheMock.Setup(m => m.TryGetValue(key, out cachedValue))
                .Returns(true);

            // Act
            var result = _service.GetOrSet(key, () => "newValue");

            // Assert
            Assert.Equal(expectedValue, result);
            _memoryCacheMock.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void GetOrSet_ExecutesFactoryAndCaches_WhenNotExists()
        {
            // Arrange
            var key = "testKey";
            var newValue = "newValue";
            var cacheEntryMock = new Mock<ICacheEntry>();

            // Setup TryGetValue to return false (cache miss)
            object? outValue = null;
            _memoryCacheMock.Setup(m => m.TryGetValue(key, out outValue))
                .Returns(false);

            // Setup CreateEntry to return our mock entry
            _memoryCacheMock.Setup(m => m.CreateEntry(key))
                .Returns(cacheEntryMock.Object);

            // Act
            var result = _service.GetOrSet(key, () => newValue);

            // Assert
            Assert.Equal(newValue, result);
            _memoryCacheMock.Verify(m => m.CreateEntry(key), Times.Once);
            cacheEntryMock.VerifySet(e => e.SlidingExpiration = TimeSpan.FromHours(1));
        }

        [Fact]
        public void GetOrSet_SetsCorrectExpiration_FromConfiguration()
        {
            // Arrange
            var key = "testKey";
            var newValue = "newValue";
            var cacheEntryMock = new Mock<ICacheEntry>();

            // Set custom expiration in config
            _configurationMock.Setup(c => c["MemoryCacheSetting:MemoryCacheExpiryHours"])
                .Returns("2.5");

            object? outValue = null;
            _memoryCacheMock.Setup(m => m.TryGetValue(key, out outValue))
                .Returns(false);

            _memoryCacheMock.Setup(m => m.CreateEntry(key))
                .Returns(cacheEntryMock.Object);

            // Act
            var result = _service.GetOrSet(key, () => newValue);

            // Assert
            Assert.Equal(newValue, result);
            cacheEntryMock.VerifySet(e => e.SlidingExpiration = TimeSpan.FromHours(2.5));
        }

        [Fact]
        public void GetOrSet_ThrowsKeyNotFoundException_WhenFactoryReturnsNull()
        {
            // Arrange
            var key = "testKey";
            object? outValue = null;
            _memoryCacheMock.Setup(m => m.TryGetValue(key, out outValue))
                .Returns(false);

            _memoryCacheMock.Setup(m => m.CreateEntry(key))
                .Returns(Mock.Of<ICacheEntry>());

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() =>
                _service.GetOrSet<string>(key, () => null!));
        }

        [Fact]
        public void GetOrSet_Throws_WhenConfigurationInvalid()
        {
            // Arrange
            var key = "testKey";
            _configurationMock.Setup(c => c["MemoryCacheSetting:MemoryCacheExpiryHours"])
                .Returns("invalid");

            // Act & Assert
            Assert.Throws<FormatException>(() =>
                _service.GetOrSet(key, () => "value"));
        }

        [Fact]
        public void Clear_RemovesKeyFromCache()
        {
            // Arrange
            var key = "testKey";

            // Act
            _service.Clear(key);

            // Assert
            _memoryCacheMock.Verify(m => m.Remove(key), Times.Once);
        }

        [Fact]
        public void Clear_DoesNotThrow_WhenKeyNotExists()
        {
            // Arrange
            var key = "nonExistentKey";
            _memoryCacheMock.Setup(m => m.Remove(key))
                .Verifiable();

            // Act & Assert (should not throw)
            _service.Clear(key);
        }

        [Fact]
        public void GetOrSet_WorksWithValueTypes()
        {
            // Arrange
            var key = "intKey";
            var expectedValue = 42;
            var cacheEntryMock = new Mock<ICacheEntry>();

            object? outValue = null;
            _memoryCacheMock.Setup(m => m.TryGetValue(key, out outValue))
                .Returns(false);

            _memoryCacheMock.Setup(m => m.CreateEntry(key))
                .Returns(cacheEntryMock.Object);

            // Act
            var result = _service.GetOrSet(key, () => expectedValue);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public void GetOrSet_WorksWithComplexObjects()
        {
            // Arrange
            var key = "objectKey";
            var expectedValue = new { Id = 1, Name = "Test" };
            var cacheEntryMock = new Mock<ICacheEntry>();

            object? outValue = null;
            _memoryCacheMock.Setup(m => m.TryGetValue(key, out outValue))
                .Returns(false);

            _memoryCacheMock.Setup(m => m.CreateEntry(key))
                .Returns(cacheEntryMock.Object);

            // Act
            var result = _service.GetOrSet(key, () => expectedValue);

            // Assert
            Assert.Equal(expectedValue, result);
        }
    }
}