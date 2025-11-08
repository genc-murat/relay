using System;
using System.Linq.Expressions;
using Xunit;

namespace Relay.Core.Testing.Tests.Builders;

public class RequestBuilderTests
{
    [Fact]
    public void Constructor_CallsWithDefaults()
    {
        // Arrange & Act
        var builder = new RequestBuilder<TestRequest>();

        // Assert
        var request = builder.Build();
        Assert.NotNull(request);
    }

    [Fact]
    public void WithDefaults_ReturnsBuilderInstance()
    {
        // Arrange
        var builder = new RequestBuilder<TestRequest>();

        // Act
        var result = builder.WithDefaults();

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithProperty_ValidExpression_SetsPropertyValue()
    {
        // Arrange
        var builder = new RequestBuilder<TestRequestWithMessage>();
        var expectedMessage = "Test Message";

        // Act
        builder.WithProperty(r => r.Message, expectedMessage);

        // Assert
        var request = builder.Build();
        Assert.Equal(expectedMessage, request.Message);
    }

    [Fact]
    public void WithProperty_InvalidExpression_ThrowsArgumentException()
    {
        // Arrange
        var builder = new RequestBuilder<TestRequestWithMessage>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            builder.WithProperty<string>(r => r.Message.Length.ToString(), "value"));
        Assert.Contains("Expression must be a member expression", exception.Message);
    }



    [Fact]
    public void Build_ReturnsInstance()
    {
        // Arrange
        var builder = new RequestBuilder<TestRequest>();

        // Act
        var request = builder.Build();

        // Assert
        Assert.NotNull(request);
        Assert.IsType<TestRequest>(request);
    }

    // Test request classes
    public class TestRequest
    {
    }

    public class TestRequestWithMessage
    {
        public string Message { get; set; } = string.Empty;
    }
}