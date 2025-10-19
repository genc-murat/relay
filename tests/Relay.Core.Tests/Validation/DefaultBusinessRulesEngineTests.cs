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

/// <summary>
/// Tests for DefaultBusinessRulesEngine class
/// </summary>
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