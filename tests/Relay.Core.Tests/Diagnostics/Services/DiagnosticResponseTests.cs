using System;
using Relay.Core.Diagnostics.Services;
using Xunit;

namespace Relay.Core.Tests.Diagnostics.Services;

public class DiagnosticResponseTests
{
    #region Generic DiagnosticResponse<T> Tests

    [Fact]
    public void Success_ShouldCreateSuccessfulResponse_WithData()
    {
        // Arrange
        var data = "test data";
        var statusCode = 201;

        // Act
        var response = DiagnosticResponse<string>.Success(data, statusCode);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(data, response.Data);
        Assert.Null(response.ErrorMessage);
        Assert.Null(response.ErrorDetails);
    }

    [Fact]
    public void Success_ShouldUseDefaultStatusCode_WhenNotSpecified()
    {
        // Arrange
        var data = 42;

        // Act
        var response = DiagnosticResponse<int>.Success(data);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(data, response.Data);
    }

    [Fact]
    public void Error_ShouldCreateErrorResponse_WithMessage()
    {
        // Arrange
        var message = "Test error";
        var statusCode = 400;

        // Act
        var response = DiagnosticResponse<string>.Error(message, null, statusCode);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(message, response.ErrorMessage);
        Assert.Null(response.Data);
        Assert.Null(response.ErrorDetails);
    }

    [Fact]
    public void Error_ShouldIncludeExceptionDetails_WhenExceptionProvided()
    {
        // Arrange
        var message = "Test error";
        InvalidOperationException exception;
        try
        {
            throw new InvalidOperationException("Inner error");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // Act
        var response = DiagnosticResponse<string>.Error(message, exception);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Equal(message, response.ErrorMessage);
        Assert.NotNull(response.ErrorDetails);

        // Check error details structure
        var details = response.ErrorDetails;
        Assert.NotNull(details);
        // The anonymous object should have Type, Message, and StackTrace properties
        var type = details.GetType().GetProperty("Type")?.GetValue(details);
        var msg = details.GetType().GetProperty("Message")?.GetValue(details);
        var stack = details.GetType().GetProperty("StackTrace")?.GetValue(details);

        Assert.Equal("InvalidOperationException", type);
        Assert.Equal("Inner error", msg);
        Assert.NotNull(stack);
    }

    [Fact]
    public void NotFound_ShouldCreateNotFoundResponse()
    {
        // Arrange
        var message = "Custom not found message";

        // Act
        var response = DiagnosticResponse<object>.NotFound(message);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(404, response.StatusCode);
        Assert.Equal(message, response.ErrorMessage);
        Assert.Null(response.Data);
        Assert.Null(response.ErrorDetails);
    }

    [Fact]
    public void NotFound_ShouldUseDefaultMessage_WhenNotSpecified()
    {
        // Act
        var response = DiagnosticResponse<object>.NotFound();

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(404, response.StatusCode);
        Assert.Equal("Resource not found", response.ErrorMessage);
    }

    [Fact]
    public void BadRequest_ShouldCreateBadRequestResponse()
    {
        // Arrange
        var message = "Invalid input";

        // Act
        var response = DiagnosticResponse<string>.BadRequest(message);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(message, response.ErrorMessage);
        Assert.Null(response.Data);
        Assert.Null(response.ErrorDetails);
    }

    [Fact]
    public void ServiceUnavailable_ShouldCreateServiceUnavailableResponse()
    {
        // Arrange
        var message = "Service is down";

        // Act
        var response = DiagnosticResponse<int>.ServiceUnavailable(message);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(503, response.StatusCode);
        Assert.Equal(message, response.ErrorMessage);
        Assert.Equal(0, response.Data); // default int is 0
        Assert.Null(response.ErrorDetails);
    }

    #endregion

    #region Non-Generic DiagnosticResponse Tests

    [Fact]
    public void NonGenericSuccess_ShouldCreateSuccessfulResponse_NoData()
    {
        // Arrange
        var statusCode = 204;

        // Act
        var response = DiagnosticResponse.Success(statusCode);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Null(response.Data);
        Assert.Null(response.ErrorMessage);
        Assert.Null(response.ErrorDetails);
    }

    [Fact]
    public void NonGenericSuccess_ShouldUseDefaultStatusCode_NoData()
    {
        // Act
        var response = DiagnosticResponse.Success();

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.StatusCode);
        Assert.Null(response.Data);
    }

    [Fact]
    public void NonGenericSuccess_WithMessage_ShouldCreateResponseWithData()
    {
        // Arrange
        var message = "Operation completed";
        var statusCode = 202;

        // Act
        var response = DiagnosticResponse.Success(message, statusCode);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(statusCode, response.StatusCode);
        Assert.NotNull(response.Data);

        // Check that data contains the message
        var data = response.Data;
        Assert.NotNull(data);
        var msg = data.GetType().GetProperty("message")?.GetValue(data);
        Assert.Equal(message, msg);
    }

    [Fact]
    public void NonGenericSuccess_WithMessage_ShouldUseDefaultStatusCode()
    {
        // Arrange
        var message = "Success";

        // Act
        var response = DiagnosticResponse.Success(message);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Data);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var response = new DiagnosticResponse<string>();

        // Act
        response.IsSuccess = true;
        response.StatusCode = 201;
        response.Data = "test";
        response.ErrorMessage = "error";
        response.ErrorDetails = new { code = 123 };

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(201, response.StatusCode);
        Assert.Equal("test", response.Data);
        Assert.Equal("error", response.ErrorMessage);
        Assert.NotNull(response.ErrorDetails);
    }

    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        // Act
        var response = new DiagnosticResponse<int>();

        // Assert
        Assert.False(response.IsSuccess); // default bool is false
        Assert.Equal(0, response.StatusCode); // default int is 0
        Assert.Equal(0, response.Data); // default int is 0
        Assert.Null(response.ErrorMessage);
        Assert.Null(response.ErrorDetails);
    }

    #endregion
}
