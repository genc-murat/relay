using System;
using System.Linq.Expressions;
using Xunit;

namespace Relay.Core.Testing.Tests.Builders;

public class ResponseBuilderTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsSuccessAndMessage()
    {
        // Arrange & Act
        var builder = new ResponseBuilder<TestResponseWithSuccessAndMessage>();

        // Assert
        var response = builder.Build();
        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
    }

    [Fact]
    public void Constructor_WithDefaults_SetsIsSuccess()
    {
        // Arrange & Act
        var builder = new ResponseBuilder<TestResponseWithIsSuccess>();

        // Assert
        var response = builder.Build();
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public void WithDefaults_ReturnsBuilderInstance()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponse>();

        // Act
        var result = builder.WithDefaults();

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithProperty_ValidExpression_SetsPropertyValue()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponseWithMessage>();
        var expectedMessage = "Custom Message";

        // Act
        builder.WithProperty(r => r.Message, expectedMessage);

        // Assert
        var response = builder.Build();
        Assert.Equal(expectedMessage, response.Message);
    }

    [Fact]
    public void WithProperty_InvalidExpression_ThrowsArgumentException()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponseWithMessage>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            builder.WithProperty<string>(r => r.Message.Length.ToString(), "value"));
        Assert.Contains("Expression must be a member expression", exception.Message);
    }

    [Fact]
    public void WithSuccess_SetsSuccessProperties()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponseWithSuccessAndIsSuccess>();

        // Act
        builder.WithSuccess();

        // Assert
        var response = builder.Build();
        Assert.True(response.Success);
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public void WithFailure_SetsFailurePropertiesWithDefaultMessage()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponseWithSuccessAndMessage>();

        // Act
        builder.WithFailure();

        // Assert
        var response = builder.Build();
        Assert.False(response.Success);
        Assert.Equal("Operation failed", response.Message);
    }

    [Fact]
    public void WithFailure_SetsFailurePropertiesWithCustomMessage()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponseWithSuccessAndErrorMessage>();
        var customMessage = "Custom failure message";

        // Act
        builder.WithFailure(customMessage);

        // Assert
        var response = builder.Build();
        Assert.False(response.Success);
        Assert.Equal(customMessage, response.ErrorMessage);
    }

    [Fact]
    public void Build_ReturnsInstance()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponse>();

        // Act
        var response = builder.Build();

        // Assert
        Assert.NotNull(response);
        Assert.IsType<TestResponse>(response);
    }

    // Test response classes
    public class TestResponse
    {
    }

    public class TestResponseWithSuccessAndMessage
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TestResponseWithIsSuccess
    {
        public bool IsSuccess { get; set; }
    }

    public class TestResponseWithMessage
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TestResponseWithSuccessAndIsSuccess
    {
        public bool Success { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class TestResponseWithSuccessAndErrorMessage
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}