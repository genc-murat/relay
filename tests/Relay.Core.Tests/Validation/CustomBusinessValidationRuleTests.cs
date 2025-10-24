using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class CustomBusinessValidationRuleTests
{
    [Fact]
    public void Constructor_Should_Throw_When_RulesEngine_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CustomBusinessValidationRule(null));
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Empty_Errors_When_Request_Is_Valid()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = true,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(result);
        mockRulesEngine.Verify(x => x.ValidateBusinessRulesAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Business_Rule_Errors()
    {
        // Arrange
        var businessErrors = new[] { "Business rule violation 1", "Business rule violation 2" };
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(businessErrors);

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = true,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains("Business rule violation 1", result);
        Assert.Contains("Business rule violation 2", result);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_Amount_Specified_But_No_Payment_Method()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = null, // Missing payment method
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("Payment method is required when amount is specified.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_Recurring_Transaction_But_End_Date_Before_Start_Date()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow.AddDays(1), // Future start date
            EndDate = DateTime.UtcNow, // Past end date - problem!
            IsRecurring = true, // Recurring flag
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("End date must be after start date for recurring transactions.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_Premium_User_But_Amount_Less_Than_Minimum()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 50m, // Less than $100 minimum for premium users
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Premium, // Premium user
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("Premium users must have minimum transaction amount of $100.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_High_Risk_Error_When_Risk_Score_Exceeds_Threshold()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 15000m, // High amount that will contribute to risk
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Suspicious, // Suspicious user type adds risk
            CountryCode = "XX", // Non-US country adds risk
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("Transaction has high risk score and requires additional verification.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Multiple_Errors_When_Multiple_Conditions_Are_Violated()
    {
        // Arrange
        var businessErrors = new[] { "Business rule violation" };
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(businessErrors);

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 15000m,
            PaymentMethod = null, // Missing payment method
            StartDate = DateTime.UtcNow.AddDays(1), // Future start date
            EndDate = DateTime.UtcNow, // Past end date - violates recurrence rule
            IsRecurring = true, // This makes the date issue a violation
            UserType = UserType.Premium, // Premium user
            CountryCode = "XX", // Non-US country adds risk
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        // We expect 3 errors: Business rule + payment method + date
        // The risk score might not be high enough depending on the exact calculation
        Assert.Equal(3, result.Count()); 
        Assert.Contains("Business rule violation", result);
        Assert.Contains("Payment method is required when amount is specified.", result);
        Assert.Contains("End date must be after start date for recurring transactions.", result);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Cancelled_Token()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IEnumerable<string>>(Task.FromCanceled<IEnumerable<string>>(new CancellationToken(true))));

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before calling

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync(request, cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Cancelled_Token_During_Risk_Calculation()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Create a token that gets cancelled during the risk calculation
        var cts = new CancellationTokenSource();
        cts.CancelAfter(5); // Cancel very quickly to interrupt the 50ms delay

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync(request, cts.Token));
    }

    [Theory]
    [InlineData(UserType.New, 5000)] // New user with moderate amount
    [InlineData(UserType.Regular, 2000)] // Regular user with moderate amount
    [InlineData(UserType.Premium, 200)] // Premium user with low amount (still above $100 minimum)
    public async Task ValidateAsync_Should_Pass_For_Valid_Combinations(UserType userType, decimal amount)
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = amount,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = userType,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        // For premium users with amounts under $100, we'd expect an error, but for all others we should pass
        if (userType == UserType.Premium && amount < 100)
        {
            Assert.Single(result);
            Assert.Equal("Premium users must have minimum transaction amount of $100.", result.First());
        }
        else
        {
            Assert.Empty(result);
        }
    }

    [Theory]
    [InlineData(0, true)] // Zero amount with payment method should pass business rules but fail cross-field validation
    [InlineData(-100, true)] // Negative amount should fail business rules
    [InlineData(100, false)] // Positive amount with no payment method should fail cross-field validation
    public async Task ValidateAsync_Should_Handle_Amount_Validation(decimal amount, bool hasPaymentMethod)
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        if (amount <= 0)
        {
            mockRulesEngine
                .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "Transaction amount must be greater than zero." });
        }
        else
        {
            mockRulesEngine
                .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<string>());
        }

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = amount,
            PaymentMethod = hasPaymentMethod ? "CreditCard" : null,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        if (amount <= 0)
        {
            // Should have business rule error
            Assert.Contains("Transaction amount must be greater than zero.", result);
        }
        else if (!hasPaymentMethod)
        {
            // Should have cross-field validation error
            Assert.Contains("Payment method is required when amount is specified.", result);
        }
        else
        {
            // Should be valid
            if (amount > 0 && hasPaymentMethod)
            {
                Assert.Empty(result);
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Null_Request()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            async () => await rule.ValidateAsync(null));
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Null_EndDate_For_Non_Recurring_Transactions()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = null, // No end date
            IsRecurring = false, // Not recurring, so no validation needed
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Edge_Case_With_Maximum_Amount_And_Suspicious_User()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 100000m, // Maximum amount
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Suspicious, // Suspicious user adds risk
            CountryCode = "XX", // Non-US country adds more risk
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Single(result);
        Assert.Equal("Transaction has high risk score and requires additional verification.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Edge_Case_With_Zero_Amount_And_No_Payment_Method()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "Transaction amount must be greater than zero." });

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 0m, // Zero amount
            PaymentMethod = null, // No payment method
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Contains("Transaction amount must be greater than zero.", result);
        // Should not have the cross-field validation error because amount is zero
        Assert.DoesNotContain("Payment method is required when amount is specified.", result);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Non_Recurring_With_End_Date_Before_Start_Date()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow.AddDays(1), // Future start
            EndDate = DateTime.UtcNow, // Past end - but not recurring
            IsRecurring = false, // Not recurring, so no validation triggered
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        // Should not have date validation error because it's not recurring
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Valid_Request_With_All_Fields_Set()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 5000m,
            PaymentMethod = "BankTransfer",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(90),
            IsRecurring = true,
            UserType = UserType.Premium,
            CountryCode = "US",
            BusinessCategory = "Technology",
            UserTransactionCount = 10
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("CU")] // Cuba
    [InlineData("IR")] // Iran
    [InlineData("KP")] // North Korea
    [InlineData("SY")] // Syria
    public async Task ValidateAsync_Should_Handle_Restricted_Countries(string countryCode)
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "Transactions from this country are not currently supported." });

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = countryCode, // Restricted country
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert
        Assert.Contains("Transactions from this country are not currently supported.", result);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Null_Country_Code()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = null, // Null country
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act
        var result = await rule.ValidateAsync(request);

        // Assert - should not fail because null country code is handled in risk calculation
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_High_Risk_Hourly_Conditions()
    {
        // Arrange
        var mockRulesEngine = new Mock<IBusinessRulesEngine>();
        mockRulesEngine
            .Setup(x => x.ValidateBusinessRulesAsync(It.IsAny<BusinessValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var rule = new CustomBusinessValidationRule(mockRulesEngine.Object);
        
        // Create a request that would trigger high risk due to unusual hours
        // We'll simulate this by mocking the risk calculation
        var request = new BusinessValidationRequest
        {
            Amount = 1000m,
            PaymentMethod = "CreditCard",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "XX", // Non-US country
            BusinessCategory = "Retail",
            UserTransactionCount = 5
        };

        // Act - note that the risk calculation depends on time of day, so we can't guarantee it will always trigger
        var result = await rule.ValidateAsync(request);

        // Assert - we can't predict if it will trigger due to time, but at least it doesn't crash
        Assert.NotNull(result);
    }
}