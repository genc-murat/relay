using System;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.Testing;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Testing;

/// <summary>
/// Tests for MockContractValidator.
/// </summary>
public class MockContractValidatorTests
{
    private readonly MockContractValidator _mock = new();
    private readonly JsonSchemaContract _schema = new() { Schema = @"{ ""type"": ""object"" }" };

    public class TestRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task ValidateRequestAsync_ShouldRecordCall()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        _mock.SetupRequestSuccess();

        // Act
        await _mock.ValidateRequestAsync(request, _schema);

        // Assert
        Assert.Equal(1, _mock.RequestValidationCallCount);
        Assert.Equal(0, _mock.ResponseValidationCallCount);
        Assert.Single(_mock.Calls);
    }

    [Fact]
    public async Task ValidateResponseAsync_ShouldRecordCall()
    {
        // Arrange
        var response = new TestRequest { Name = "Test" };
        _mock.SetupResponseSuccess();

        // Act
        await _mock.ValidateResponseAsync(response, _schema);

        // Assert
        Assert.Equal(0, _mock.RequestValidationCallCount);
        Assert.Equal(1, _mock.ResponseValidationCallCount);
        Assert.Single(_mock.Calls);
    }

    [Fact]
    public async Task SetupRequestSuccess_ShouldReturnNoErrors()
    {
        // Arrange
        var request = new TestRequest();
        _mock.SetupRequestSuccess();

        // Act
        var errors = await _mock.ValidateRequestAsync(request, _schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task SetupRequestFailure_ShouldReturnError()
    {
        // Arrange
        var request = new TestRequest();
        _mock.SetupRequestFailure("CV001", "Validation failed");

        // Act
        var errors = await _mock.ValidateRequestAsync(request, _schema);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Validation failed", errors.First());
    }

    [Fact]
    public async Task SetupResponseSuccess_ShouldReturnNoErrors()
    {
        // Arrange
        var response = new TestRequest();
        _mock.SetupResponseSuccess();

        // Act
        var errors = await _mock.ValidateResponseAsync(response, _schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task SetupResponseFailure_ShouldReturnError()
    {
        // Arrange
        var response = new TestRequest();
        _mock.SetupResponseFailure("CV002", "Response validation failed");

        // Act
        var errors = await _mock.ValidateResponseAsync(response, _schema);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Response validation failed", errors.First());
    }

    [Fact]
    public async Task SetupRequestValidation_WithCustomFunc_ShouldUseFunc()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var callCount = 0;
        
        _mock.SetupRequestValidation((obj, schema) =>
        {
            callCount++;
            return ValidationResult.Success();
        });

        // Act
        await _mock.ValidateRequestAsync(request, _schema);

        // Assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SetupResponseValidation_WithCustomFunc_ShouldUseFunc()
    {
        // Arrange
        var response = new TestRequest { Name = "Test" };
        var callCount = 0;
        
        _mock.SetupResponseValidation((obj, schema) =>
        {
            callCount++;
            return ValidationResult.Success();
        });

        // Act
        await _mock.ValidateResponseAsync(response, _schema);

        // Assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task VerifyRequestValidated_WithMatchingRequest_ShouldReturnTrue()
    {
        // Arrange
        var request = new TestRequest();
        _mock.SetupRequestSuccess();
        await _mock.ValidateRequestAsync(request, _schema);

        // Act
        var verified = _mock.VerifyRequestValidated(request);

        // Assert
        Assert.True(verified);
    }

    [Fact]
    public void VerifyRequestValidated_WithoutCall_ShouldReturnFalse()
    {
        // Arrange
        var request = new TestRequest();

        // Act
        var verified = _mock.VerifyRequestValidated(request);

        // Assert
        Assert.False(verified);
    }

    [Fact]
    public async Task VerifyResponseValidated_WithMatchingResponse_ShouldReturnTrue()
    {
        // Arrange
        var response = new TestRequest();
        _mock.SetupResponseSuccess();
        await _mock.ValidateResponseAsync(response, _schema);

        // Act
        var verified = _mock.VerifyResponseValidated(response);

        // Assert
        Assert.True(verified);
    }

    [Fact]
    public void VerifyResponseValidated_WithoutCall_ShouldReturnFalse()
    {
        // Arrange
        var response = new TestRequest();

        // Act
        var verified = _mock.VerifyResponseValidated(response);

        // Assert
        Assert.False(verified);
    }

    [Fact]
    public async Task GetLastCall_WithCalls_ShouldReturnLastCall()
    {
        // Arrange
        var request1 = new TestRequest { Name = "First" };
        var request2 = new TestRequest { Name = "Second" };
        _mock.SetupRequestSuccess();

        // Act
        await _mock.ValidateRequestAsync(request1, _schema);
        await _mock.ValidateRequestAsync(request2, _schema);
        var lastCall = _mock.GetLastCall();

        // Assert
        Assert.NotNull(lastCall);
        Assert.Same(request2, lastCall.Object);
        Assert.True(lastCall.IsRequest);
    }

    [Fact]
    public void GetLastCall_WithoutCalls_ShouldReturnNull()
    {
        // Act
        var lastCall = _mock.GetLastCall();

        // Assert
        Assert.Null(lastCall);
    }

    [Fact]
    public async Task Reset_ShouldClearCallsAndConfiguration()
    {
        // Arrange
        var request = new TestRequest();
        _mock.SetupRequestFailure("CV001", "Error");
        await _mock.ValidateRequestAsync(request, _schema);

        // Act
        _mock.Reset();

        // Assert
        Assert.Empty(_mock.Calls);
        Assert.Equal(0, _mock.RequestValidationCallCount);
        
        // Verify configuration is reset (should return success by default)
        var errors = await _mock.ValidateRequestAsync(request, _schema);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateRequestDetailedAsync_ShouldRecordCall()
    {
        // Arrange
        var request = new TestRequest();
        var context = ValidationContext.ForRequest(typeof(TestRequest), request, _schema);
        _mock.SetupRequestSuccess();

        // Act
        var result = await _mock.ValidateRequestDetailedAsync(request, _schema, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, _mock.RequestValidationCallCount);
    }

    [Fact]
    public async Task ValidateResponseDetailedAsync_ShouldRecordCall()
    {
        // Arrange
        var response = new TestRequest();
        var context = ValidationContext.ForResponse(typeof(TestRequest), response, _schema);
        _mock.SetupResponseSuccess();

        // Act
        var result = await _mock.ValidateResponseDetailedAsync(response, _schema, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, _mock.ResponseValidationCallCount);
    }

    [Fact]
    public async Task Calls_ShouldContainCallDetails()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        _mock.SetupRequestSuccess();

        // Act
        await _mock.ValidateRequestAsync(request, _schema);
        var call = _mock.Calls.First();

        // Assert
        Assert.Same(request, call.Object);
        Assert.Same(_schema, call.Schema);
        Assert.True(call.IsRequest);
        Assert.True(call.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task MultipleCalls_ShouldTrackAllCalls()
    {
        // Arrange
        var request = new TestRequest();
        var response = new TestRequest();
        _mock.SetupRequestSuccess();
        _mock.SetupResponseSuccess();

        // Act
        await _mock.ValidateRequestAsync(request, _schema);
        await _mock.ValidateResponseAsync(response, _schema);
        await _mock.ValidateRequestAsync(request, _schema);

        // Assert
        Assert.Equal(3, _mock.Calls.Count);
        Assert.Equal(2, _mock.RequestValidationCallCount);
        Assert.Equal(1, _mock.ResponseValidationCallCount);
    }
}
