using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for CachedUsernameUniquenessChecker class
/// </summary>
public class CachedUsernameUniquenessCheckerTests
{
    private readonly Mock<IUsernameUniquenessChecker> _mockInnerChecker;
    private readonly Mock<ICache> _mockCache;
    private readonly CachedUsernameUniquenessChecker _checker;

    public CachedUsernameUniquenessCheckerTests()
    {
        _mockInnerChecker = new Mock<IUsernameUniquenessChecker>();
        _mockCache = new Mock<ICache>();
        _checker = new CachedUsernameUniquenessChecker(_mockInnerChecker.Object, _mockCache.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var checker = new CachedUsernameUniquenessChecker(_mockInnerChecker.Object, _mockCache.Object);

        // Assert
        Assert.NotNull(checker);
    }

    [Fact]
    public void Constructor_WithNullInnerChecker_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CachedUsernameUniquenessChecker(null!, _mockCache.Object));
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CachedUsernameUniquenessChecker(_mockInnerChecker.Object, null!));
    }

    #endregion

    #region Cache Hit Tests

    [Fact]
    public async Task IsUsernameUniqueAsync_CacheHit_ReturnsCachedValue()
    {
        // Arrange
        var username = "testuser";
        var cachedResult = true;
        var cacheKey = $"username_unique_{username}";

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(cachedResult));

        // Act
        var result = await _checker.IsUsernameUniqueAsync(username);

        // Assert
        Assert.Equal(cachedResult, result);
        _mockInnerChecker.Verify(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsUsernameUniqueAsync_CacheHitFalse_ReturnsFalse()
    {
        // Arrange
        var username = "existinguser";
        var cachedResult = false;
        var cacheKey = $"username_unique_{username}";

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(cachedResult));

        // Act
        var result = await _checker.IsUsernameUniqueAsync(username);

        // Assert
        Assert.Equal(cachedResult, result);
        _mockInnerChecker.Verify(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsUsernameUniqueAsync_CacheHit_WithCancellationToken_UsesToken()
    {
        // Arrange
        var username = "testuser";
        var cachedResult = true;
        var cacheKey = $"username_unique_{username}";
        var cancellationToken = new CancellationTokenSource().Token;

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, cancellationToken))
            .Returns(ValueTask.FromResult<bool?>(cachedResult));

        // Act
        var result = await _checker.IsUsernameUniqueAsync(username, cancellationToken);

        // Assert
        Assert.Equal(cachedResult, result);
        _mockCache.Verify(x => x.GetAsync<bool?>(cacheKey, cancellationToken), Times.Once);
    }

    #endregion

    #region Cache Miss Tests

    [Fact]
    public async Task IsUsernameUniqueAsync_CacheMiss_ChecksInnerAndCachesResult()
    {
        // Arrange
        var username = "newuser";
        var innerResult = true;
        var cacheKey = $"username_unique_{username}";
        var expectedExpiration = TimeSpan.FromMinutes(5);

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(null));

        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username, default))
            .Returns(ValueTask.FromResult(innerResult));

        // Act
        var result = await _checker.IsUsernameUniqueAsync(username);

        // Assert
        Assert.Equal(innerResult, result);
        _mockInnerChecker.Verify(x => x.IsUsernameUniqueAsync(username, default), Times.Once);
        _mockCache.Verify(x => x.SetAsync(cacheKey, innerResult, expectedExpiration, default), Times.Once);
    }

    [Fact]
    public async Task IsUsernameUniqueAsync_CacheMissFalse_CachesFalseResult()
    {
        // Arrange
        var username = "existinguser";
        var innerResult = false;
        var cacheKey = $"username_unique_{username}";
        var expectedExpiration = TimeSpan.FromMinutes(5);

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(null));

        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username, default))
            .Returns(ValueTask.FromResult(innerResult));

        // Act
        var result = await _checker.IsUsernameUniqueAsync(username);

        // Assert
        Assert.Equal(innerResult, result);
        _mockCache.Verify(x => x.SetAsync(cacheKey, innerResult, expectedExpiration, default), Times.Once);
    }

    [Fact]
    public async Task IsUsernameUniqueAsync_CacheMiss_WithCancellationToken_PassesTokenToBoth()
    {
        // Arrange
        var username = "newuser";
        var innerResult = true;
        var cacheKey = $"username_unique_{username}";
        var cancellationToken = new CancellationTokenSource().Token;
        var expectedExpiration = TimeSpan.FromMinutes(5);

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, cancellationToken))
            .Returns(ValueTask.FromResult<bool?>(null));

        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username, cancellationToken))
            .Returns(ValueTask.FromResult(innerResult));

        // Act
        var result = await _checker.IsUsernameUniqueAsync(username, cancellationToken);

        // Assert
        Assert.Equal(innerResult, result);
        _mockInnerChecker.Verify(x => x.IsUsernameUniqueAsync(username, cancellationToken), Times.Once);
        _mockCache.Verify(x => x.SetAsync(cacheKey, innerResult, expectedExpiration, cancellationToken), Times.Once);
    }

    #endregion

    #region Cache Key Generation Tests

    [Fact]
    public async Task IsUsernameUniqueAsync_GeneratesCorrectCacheKey()
    {
        // Arrange
        var username = "TestUser123";
        var expectedCacheKey = $"username_unique_{username}";
        var innerResult = true;

        _mockCache.Setup(x => x.GetAsync<bool?>(expectedCacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(null));

        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username, default))
            .Returns(ValueTask.FromResult(innerResult));

        // Act
        await _checker.IsUsernameUniqueAsync(username);

        // Assert
        _mockCache.Verify(x => x.GetAsync<bool?>(expectedCacheKey, default), Times.Once);
        _mockCache.Verify(x => x.SetAsync(expectedCacheKey, innerResult, It.IsAny<TimeSpan>(), default), Times.Once);
    }

    [Fact]
    public async Task IsUsernameUniqueAsync_SpecialCharactersInUsername_HandledCorrectly()
    {
        // Arrange
        var username = "user@domain.com";
        var expectedCacheKey = $"username_unique_{username}";
        var innerResult = true;

        _mockCache.Setup(x => x.GetAsync<bool?>(expectedCacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(null));

        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username, default))
            .Returns(ValueTask.FromResult(innerResult));

        // Act
        await _checker.IsUsernameUniqueAsync(username);

        // Assert
        _mockCache.Verify(x => x.GetAsync<bool?>(expectedCacheKey, default), Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task IsUsernameUniqueAsync_CacheThrowsException_PropagatesException()
    {
        // Arrange
        var username = "testuser";
        var cacheKey = $"username_unique_{username}";
        var expectedException = new InvalidOperationException("Cache error");

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, default))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _checker.IsUsernameUniqueAsync(username).AsTask());

        Assert.Equal("Cache error", exception.Message);
        _mockInnerChecker.Verify(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsUsernameUniqueAsync_InnerCheckerThrowsException_PropagatesException()
    {
        // Arrange
        var username = "testuser";
        var cacheKey = $"username_unique_{username}";
        var expectedException = new InvalidOperationException("Database error");

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(null));

        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username, default))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _checker.IsUsernameUniqueAsync(username).AsTask());

        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    public async Task IsUsernameUniqueAsync_CacheSetThrowsException_PropagatesException()
    {
        // Arrange
        var username = "testuser";
        var cacheKey = $"username_unique_{username}";
        var innerResult = true;
        var expectedException = new InvalidOperationException("Cache set error");

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(null));

        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username, default))
            .Returns(ValueTask.FromResult(innerResult));

        _mockCache.Setup(x => x.SetAsync(cacheKey, innerResult, It.IsAny<TimeSpan>(), default))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _checker.IsUsernameUniqueAsync(username).AsTask());

        Assert.Equal("Cache set error", exception.Message);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task IsUsernameUniqueAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var username = "testuser";
        var cacheKey = $"username_unique_{username}";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, cancellationTokenSource.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _checker.IsUsernameUniqueAsync(username, cancellationTokenSource.Token).AsTask());
    }

    #endregion

    #region Multiple Calls Tests

    [Fact]
    public async Task IsUsernameUniqueAsync_MultipleCalls_SameUsername_UsesCache()
    {
        // Arrange
        var username = "testuser";
        var cacheKey = $"username_unique_{username}";
        var cachedResult = true;

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey, default))
            .Returns(ValueTask.FromResult<bool?>(cachedResult));

        // Act
        var result1 = await _checker.IsUsernameUniqueAsync(username);
        var result2 = await _checker.IsUsernameUniqueAsync(username);
        var result3 = await _checker.IsUsernameUniqueAsync(username);

        // Assert
        Assert.Equal(cachedResult, result1);
        Assert.Equal(cachedResult, result2);
        Assert.Equal(cachedResult, result3);

        // Cache should only be accessed once per call, inner checker never called
        _mockCache.Verify(x => x.GetAsync<bool?>(cacheKey, default), Times.Exactly(3));
        _mockInnerChecker.Verify(x => x.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsUsernameUniqueAsync_DifferentUsernames_GenerateDifferentKeys()
    {
        // Arrange
        var username1 = "user1";
        var username2 = "user2";
        var cacheKey1 = $"username_unique_{username1}";
        var cacheKey2 = $"username_unique_{username2}";

        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey1, default))
            .Returns(ValueTask.FromResult<bool?>(null));
        _mockCache.Setup(x => x.GetAsync<bool?>(cacheKey2, default))
            .Returns(ValueTask.FromResult<bool?>(null));

        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username1, default))
            .Returns(ValueTask.FromResult(true));
        _mockInnerChecker.Setup(x => x.IsUsernameUniqueAsync(username2, default))
            .Returns(ValueTask.FromResult(false));

        // Act
        var result1 = await _checker.IsUsernameUniqueAsync(username1);
        var result2 = await _checker.IsUsernameUniqueAsync(username2);

        // Assert
        Assert.True(result1);
        Assert.False(result2);

        _mockCache.Verify(x => x.GetAsync<bool?>(cacheKey1, default), Times.Once);
        _mockCache.Verify(x => x.GetAsync<bool?>(cacheKey2, default), Times.Once);
        _mockInnerChecker.Verify(x => x.IsUsernameUniqueAsync(username1, default), Times.Once);
        _mockInnerChecker.Verify(x => x.IsUsernameUniqueAsync(username2, default), Times.Once);
    }

    #endregion
}