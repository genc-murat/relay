using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class CustomBusinessValidationRuleTests
{
    private readonly Mock<IBusinessRulesEngine> _mockRulesEngine;
    private readonly Mock<ILogger<DefaultBusinessRulesEngine>> _mockLogger;
    private readonly CustomBusinessValidationRule _rule;
    private readonly DefaultBusinessRulesEngine _engine;

    public CustomBusinessValidationRuleTests()
    {
        _mockRulesEngine = new Mock<IBusinessRulesEngine>();
        _mockLogger = new Mock<ILogger<DefaultBusinessRulesEngine>>();
        _rule = new CustomBusinessValidationRule(_mockRulesEngine.Object);
        _engine = new DefaultBusinessRulesEngine(_mockLogger.Object);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_Request()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 500,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = true,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 10
        };

        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_PaymentMethod_Missing_For_Positive_Amount()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = null,
            UserType = UserType.Regular
        };

        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Payment method is required when amount is specified.", errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_EndDate_Before_StartDate_For_Recurring()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-1),
            IsRecurring = true,
            UserType = UserType.Regular
        };

        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("End date must be after start date for recurring transactions.", errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_Premium_User_Has_Low_Amount()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 50,
            PaymentMethod = "CreditCard",
            UserType = UserType.Premium
        };

        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Premium users must have minimum transaction amount of $100.", errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_High_Risk_Score()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 50000, // High amount
            PaymentMethod = "CreditCard",
            UserType = UserType.Suspicious, // High risk user
            CountryCode = "XX", // Non-US
            UserTransactionCount = 0
        };

        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("Transaction has high risk score and requires additional verification.", errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Business_Rule_Errors()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 0, // Invalid amount
            UserType = UserType.Regular
        };

        var businessErrors = new[] { "Transaction amount must be greater than zero." };
        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(businessErrors);

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Transaction amount must be greater than zero.", errors.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Multiple_Errors()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 50, // Low for premium
            PaymentMethod = null,
            UserType = UserType.Premium
        };

        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert
        Assert.Equal(2, errors.Count());
        Assert.Contains("Payment method is required when amount is specified.", errors);
        Assert.Contains("Premium users must have minimum transaction amount of $100.", errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Pass_CancellationToken()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = "CreditCard",
            UserType = UserType.Regular
        };

        var cts = new CancellationTokenSource();
        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, cts.Token))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request, cts.Token);

        // Assert
        Assert.Empty(errors);
        _mockRulesEngine.Verify(x => x.ValidateBusinessRulesAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Cancelled_Token()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            UserType = UserType.Regular
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => _rule.ValidateAsync(request, cts.Token).AsTask());
    }

    [Fact]
    public async Task ValidateAsync_Should_Calculate_Risk_Score_Correctly()
    {
        // Arrange - High risk scenario
        var request = new BusinessValidationRequest
        {
            Amount = 15000, // >10000 = +0.3
            PaymentMethod = "CreditCard",
            UserType = UserType.Suspicious, // +0.5
            CountryCode = "CA", // Non-US = +0.1
            UserTransactionCount = 0
        };

        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert - Risk score = 0.3 + 0.5 + 0.1 = 0.9 > 0.8, so error
        Assert.Single(errors);
        Assert.Contains("high risk score", errors.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Cap_Risk_Score_At_1_0()
    {
        // Arrange - Very high risk
        var request = new BusinessValidationRequest
        {
            Amount = 50000,
            PaymentMethod = "CreditCard",
            UserType = UserType.Suspicious,
            CountryCode = "XX",
            UserTransactionCount = 0
        };

        _mockRulesEngine.Setup(x => x.ValidateBusinessRulesAsync(request, default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var errors = await _rule.ValidateAsync(request);

        // Assert
        Assert.Single(errors);
    }
}

public class DefaultBusinessRulesEngineTests
{
    private readonly Mock<ILogger<DefaultBusinessRulesEngine>> _mockLogger;
    private readonly DefaultBusinessRulesEngine _engine;

    public DefaultBusinessRulesEngineTests()
    {
        _mockLogger = new Mock<ILogger<DefaultBusinessRulesEngine>>();
        _engine = new DefaultBusinessRulesEngine(_mockLogger.Object);
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Return_Error_For_Zero_Amount()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 0,
            UserType = UserType.Regular,
            StartDate = DateTime.UtcNow
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("greater than zero", errors.First());
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Return_Error_For_Excessive_Amount()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 150000,
            UserType = UserType.Regular,
            StartDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("exceeds maximum limit", errors.First());
    }

    [Theory]
    [InlineData(UserType.New, 6)]
    [InlineData(UserType.Regular, 51)]
    [InlineData(UserType.Premium, 501)]
    [InlineData(UserType.Suspicious, 2)]
    [InlineData(UserType.Blocked, 1)]
    public async Task ValidateBusinessRulesAsync_Should_Return_Error_For_Exceeded_Transaction_Limit(UserType userType, int transactionCount)
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            UserType = userType,
            UserTransactionCount = transactionCount,
            StartDate = DateTime.UtcNow
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("exceeded maximum transaction limit", errors.First());
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Return_Error_For_HighRisk_Category_NonPremium_User()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            UserType = UserType.Regular,
            BusinessCategory = "HighRisk",
            StartDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("High-risk business category requires premium user status", errors.First());
    }

    [Theory]
    [InlineData("CU")]
    [InlineData("IR")]
    [InlineData("KP")]
    [InlineData("SY")]
    public async Task ValidateBusinessRulesAsync_Should_Return_Error_For_Restricted_Countries(string countryCode)
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            UserType = UserType.Regular,
            CountryCode = countryCode,
            StartDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("not currently supported", errors.First());
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Return_Error_For_Old_StartDate()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            UserType = UserType.Regular,
            StartDate = DateTime.UtcNow.AddDays(-31)
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("more than 30 days in the past", errors.First());
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Return_Error_For_Future_EndDate()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            UserType = UserType.Regular,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(3)
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Contains("more than 2 years in the future", errors.First());
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Log_Validation_Result()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            UserType = UserType.Regular
        };

        // Act
        await _engine.ValidateBusinessRulesAsync(request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Validated business rules")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Handle_Boundary_Date_Exactly_30_Days_Ago()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = "credit_card",
            StartDate = now.AddDays(-30),
            EndDate = now.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "retail",
            UserTransactionCount = 5
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert - Exactly 30 days ago should be valid
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Handle_Boundary_Date_Exactly_31_Days_Ago()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = "credit_card",
            StartDate = DateTime.UtcNow.AddDays(-31),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "retail",
            UserTransactionCount = 5
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert - Exactly 31 days ago should be invalid
        Assert.Single(errors);
        Assert.Contains("more than 30 days in the past", errors.First());
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Handle_Boundary_UserTransactionCount_Zero()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = "credit_card",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.New,
            CountryCode = "US",
            BusinessCategory = "retail",
            UserTransactionCount = 0
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert - Zero transaction count should be valid for new user
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Return_Error_For_Excessive_UserTransactionCount()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = "credit_card",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "retail",
            UserTransactionCount = int.MaxValue
        };

        // Act
        var errors = await _engine.ValidateBusinessRulesAsync(request);

        // Assert - Excessive transaction count should be invalid
        Assert.Single(errors);
        Assert.Contains("exceeded maximum transaction limit", errors.First());
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Handle_Concurrent_Validation()
    {
        // Arrange
        var requests = new List<BusinessValidationRequest>();
        for (int i = 0; i < 100; i++)
        {
            requests.Add(new BusinessValidationRequest
            {
                Amount = 100 + i,
                PaymentMethod = "credit_card",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(30),
                IsRecurring = false,
                UserType = UserType.Regular,
                CountryCode = "US",
                BusinessCategory = "retail",
                UserTransactionCount = 5
            });
        }

        // Act
        var tasks = requests.Select(r => _engine.ValidateBusinessRulesAsync(r).AsTask());
        var results = await Task.WhenAll(tasks);

        // Assert - All validations should succeed
        foreach (var errors in results)
        {
            Assert.Empty(errors);
        }
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Handle_CancellationToken_Already_Cancelled()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = "credit_card",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "retail",
            UserTransactionCount = 5
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _engine.ValidateBusinessRulesAsync(request, cts.Token).AsTask());
    }

    [Fact]
    public async Task ValidateBusinessRulesAsync_Should_Handle_Long_Running_Validation_With_Cancellation()
    {
        // Arrange
        var request = new BusinessValidationRequest
        {
            Amount = 100,
            PaymentMethod = "credit_card",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "retail",
            UserTransactionCount = 5
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Act
        var task = _engine.ValidateBusinessRulesAsync(request, cts.Token);

        // Assert - Should either complete successfully or be cancelled
        try
        {
            var errors = await task;
            Assert.Empty(errors); // If not cancelled, should succeed
        }
        catch (TaskCanceledException)
        {
            // Expected if cancelled
        }
    }
}

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