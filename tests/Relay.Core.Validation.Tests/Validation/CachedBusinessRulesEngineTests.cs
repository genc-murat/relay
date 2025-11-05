using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for CachedBusinessRulesEngine class
/// </summary>
public class CachedBusinessRulesEngineTests
{
    private readonly Mock<IBusinessRulesEngine> _mockInnerEngine;
    private readonly Mock<ICache> _mockCache;
    private readonly CachedBusinessRulesEngine _cachedEngine;

    public CachedBusinessRulesEngineTests()
    {
        _mockInnerEngine = new Mock<IBusinessRulesEngine>();
        _mockCache = new Mock<ICache>();
        _cachedEngine = new CachedBusinessRulesEngine(_mockInnerEngine.Object, _mockCache.Object);
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Return_Cached_Result_When_Available()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail"
        };

        var cachedErrors = new[] { "Cached error" };
        _mockCache.Setup(x => x.GetAsync<IEnumerable<string>>("business_rules_Regular_US_Retail", default))
            .ReturnsAsync(cachedErrors);

        // Act
        var errors = await _cachedEngine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Equal(cachedErrors, errors);
        _mockInnerEngine.Verify(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Execute_Validation_When_Not_Cached()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            UserType = UserType.Premium,
            CountryCode = "CA",
            BusinessCategory = "Finance"
        };

        var validationErrors = new[] { "Validation error" };
        _mockCache.Setup(x => x.GetAsync<IEnumerable<string>>("business_rules_Premium_CA_Finance", default))
            .ReturnsAsync((IEnumerable<string>?)null);
        _mockInnerEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(validationErrors);

        // Act
        var errors = await _cachedEngine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Equal(validationErrors, errors);
        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), TimeSpan.FromMinutes(10), default), Times.Once);
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Pass_CancellationToken()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            UserType = UserType.Regular
        };

        var cts = new CancellationTokenSource();
        _mockCache.Setup(x => x.GetAsync<IEnumerable<string>>(It.IsAny<string>(), cts.Token))
            .ReturnsAsync((IEnumerable<string>?)null);
        _mockInnerEngine.Setup(x => x.ValidateBusinessRulesAsync(request, cts.Token))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        await _cachedEngine.ValidateBusinessRulesAsync(request, cts.Token);

        // Assert
        _mockInnerEngine.Verify(x => x.ValidateBusinessRulesAsync(request, cts.Token), Times.Once);
    }
}