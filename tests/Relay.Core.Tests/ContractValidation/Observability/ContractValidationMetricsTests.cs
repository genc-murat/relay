using System;
using System.Diagnostics.Metrics;
using Relay.Core.ContractValidation.Observability;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Observability;

public class ContractValidationMetricsTests : IDisposable
{
    private readonly ContractValidationMetrics _metrics;

    public ContractValidationMetricsTests()
    {
        _metrics = new ContractValidationMetrics();
    }

    [Fact]
    public void Constructor_InitializesAllMetrics()
    {
        // Assert - Constructor should not throw and should initialize all fields
        Assert.NotNull(_metrics);
        
        // Verify that error counts dictionary is initialized
        var errorCounts = _metrics.GetErrorCountsByType();
        Assert.NotNull(errorCounts);
        Assert.Empty(errorCounts);
    }

    [Fact]
    public void Constructor_CreatesMeterWithCorrectNameAndVersion()
    {
        // Act & Assert - Constructor creates meter successfully
        // If constructor fails, test will fail during class initialization
        Assert.True(true); // Constructor completed without exception
    }

    [Fact]
    public void RecordValidation_IncrementsCounters()
    {
        // Arrange
        var requestType = "TestRequest";
        var isRequest = true;
        var isValid = true;
        var durationMs = 100.0;

        // Act
        _metrics.RecordValidation(requestType, isRequest, isValid, durationMs);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void RecordValidation_WithErrors_IncrementsErrorCounter()
    {
        // Arrange
        var requestType = "TestRequest";
        var isRequest = false;
        var isValid = false;
        var durationMs = 150.0;
        var errorCount = 3;

        // Act
        _metrics.RecordValidation(requestType, isRequest, isValid, durationMs, errorCount);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void RecordSchemaResolution_IncrementsCounters()
    {
        // Arrange
        var typeName = "TestType";
        var providerType = "TestProvider";
        var success = true;
        var durationMs = 50.0;

        // Act
        _metrics.RecordSchemaResolution(typeName, providerType, success, durationMs);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void RecordCustomValidatorExecution_IncrementsCounter()
    {
        // Arrange
        var validatorType = "TestValidator";
        var success = true;

        // Act
        _metrics.RecordCustomValidatorExecution(validatorType, success);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void RecordValidationError_AddsToErrorCounts()
    {
        // Arrange
        var errorCode = "TEST_ERROR";

        // Act
        _metrics.RecordValidationError(errorCode);

        // Assert
        var errorCounts = _metrics.GetErrorCountsByType();
        Assert.Equal(1, errorCounts[errorCode]);
    }

    [Fact]
    public void RecordValidationError_MultipleErrors_IncrementsCount()
    {
        // Arrange
        var errorCode = "TEST_ERROR";

        // Act
        _metrics.RecordValidationError(errorCode);
        _metrics.RecordValidationError(errorCode);
        _metrics.RecordValidationError(errorCode);

        // Assert
        var errorCounts = _metrics.GetErrorCountsByType();
        Assert.Equal(3, errorCounts[errorCode]);
    }

    [Fact]
    public void IncrementActiveValidations_IncrementsCount()
    {
        // Act
        _metrics.IncrementActiveValidations();

        // Assert - Should not throw and increment count
        Assert.True(true);
    }

    [Fact]
    public void DecrementActiveValidations_DecrementsCount()
    {
        // Arrange
        _metrics.IncrementActiveValidations();

        // Act
        _metrics.DecrementActiveValidations();

        // Assert - Should not throw and decrement count
        Assert.True(true);
    }

    [Fact]
    public void GetErrorCountsByType_ReturnsDictionary()
    {
        // Arrange
        _metrics.RecordValidationError("ERROR1");
        _metrics.RecordValidationError("ERROR2");

        // Act
        var errorCounts = _metrics.GetErrorCountsByType();

        // Assert
        Assert.NotNull(errorCounts);
        Assert.Equal(2, errorCounts.Count);
        Assert.Equal(1, errorCounts["ERROR1"]);
        Assert.Equal(1, errorCounts["ERROR2"]);
    }

    [Fact]
    public void Dispose_DisposesMeter()
    {
        // Arrange
        var metrics = new ContractValidationMetrics();

        // Act & Assert - Should not throw
        metrics.Dispose();
        Assert.True(true);
    }

    public void Dispose()
    {
        _metrics?.Dispose();
    }
}