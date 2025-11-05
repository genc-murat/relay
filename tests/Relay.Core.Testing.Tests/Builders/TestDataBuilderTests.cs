using System;
using System.Linq.Expressions;
using Relay.Core.Testing;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Test data classes for testing builders
/// </summary>
public class TestRequest
{
    public string Name { get; set; }
    public int Value { get; set; }
    public bool IsActive { get; set; }
}

public class TestNotification : INotification
{
    public string Message { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class TestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
}

public class TestDataBuilderTests
{
    [Fact]
    public void RequestBuilder_BuildsRequest_WithDefaults()
    {
        // Act
        var request = new RequestBuilder<TestRequest>().Build();

        // Assert
        Assert.NotNull(request);
        Assert.IsType<TestRequest>(request);
    }

    [Fact]
    public void RequestBuilder_WithProperty_SetsPropertyValue()
    {
        // Act
        var request = new RequestBuilder<TestRequest>()
            .WithProperty(r => r.Name, "Test Name")
            .WithProperty(r => r.Value, 42)
            .Build();

        // Assert
        Assert.Equal("Test Name", request.Name);
        Assert.Equal(42, request.Value);
    }

    [Fact]
    public void RequestBuilder_WithAction_ConfiguresInstance()
    {
        // Act
        var request = new RequestBuilder<TestRequest>()
            .With(r =>
            {
                r.Name = "Configured Name";
                r.IsActive = true;
            })
            .Build();

        // Assert
        Assert.Equal("Configured Name", request.Name);
        Assert.True(request.IsActive);
    }

    [Fact]
    public void NotificationBuilder_BuildsNotification_WithDefaults()
    {
        // Act
        var notification = new NotificationBuilder<TestNotification>().Build();

        // Assert
        Assert.NotNull(notification);
        Assert.IsType<TestNotification>(notification);
        Assert.True(notification.Timestamp > DateTimeOffset.MinValue);
    }

    [Fact]
    public void NotificationBuilder_WithTimestamp_SetsTimestamp()
    {
        // Arrange
        var customTimestamp = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var notification = new NotificationBuilder<TestNotification>()
            .WithTimestamp(customTimestamp)
            .Build();

        // Assert
        Assert.Equal(customTimestamp, notification.Timestamp);
    }

    [Fact]
    public void NotificationBuilder_WithProperty_SetsPropertyValue()
    {
        // Act
        var notification = new NotificationBuilder<TestNotification>()
            .WithProperty(n => n.Message, "Test Message")
            .Build();

        // Assert
        Assert.Equal("Test Message", notification.Message);
    }

    [Fact]
    public void ResponseBuilder_BuildsResponse_WithDefaults()
    {
        // Act
        var response = new ResponseBuilder<TestResponse>().Build();

        // Assert
        Assert.NotNull(response);
        Assert.IsType<TestResponse>(response);
        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
    }

    [Fact]
    public void ResponseBuilder_WithSuccess_SetsSuccessState()
    {
        // Act
        var response = new ResponseBuilder<TestResponse>()
            .WithSuccess()
            .Build();

        // Assert
        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
    }

    [Fact]
    public void ResponseBuilder_WithFailure_SetsFailureState()
    {
        // Act
        var response = new ResponseBuilder<TestResponse>()
            .WithFailure("Custom error message")
            .Build();

        // Assert
        Assert.False(response.Success);
        Assert.Equal("Custom error message", response.Message);
    }

    [Fact]
    public void ResponseBuilder_WithProperty_SetsPropertyValue()
    {
        // Act
        var response = new ResponseBuilder<TestResponse>()
            .WithProperty(r => r.Data, new { Key = "Value" })
            .Build();

        // Assert
        Assert.NotNull(response.Data);
        Assert.Equal("Value", ((dynamic)response.Data).Key);
    }

    [Fact]
    public void TestDataBuilderExtensions_WithProperty_SetsPropertyValue()
    {
        // Act
        var request = new RequestBuilder<TestRequest>()
            .WithProperty(r => r.Name, "Extension Test")
            .Build();

        // Assert
        Assert.Equal("Extension Test", request.Name);
    }

    [Fact]
    public void TestDataBuilder_ChainedCalls_WorkCorrectly()
    {
        // Act
        var request = new RequestBuilder<TestRequest>()
            .WithDefaults()
            .WithProperty(r => r.Name, "Chained")
            .WithProperty(r => r.Value, 123)
            .With(r => r.IsActive = true)
            .Build();

        // Assert
        Assert.Equal("Chained", request.Name);
        Assert.Equal(123, request.Value);
        Assert.True(request.IsActive);
    }

    [Fact]
    public void RequestBuilder_With_NullAction_DoesNothing()
    {
        // Arrange
        var builder = new RequestBuilder<TestRequest>();

        // Act
        var result = builder.With(null);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void RequestBuilder_Build_ReturnsConfiguredInstance()
    {
        // Arrange
        var builder = new RequestBuilder<TestRequest>();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestRequest>(result);
    }



    [Fact]
    public void NotificationBuilder_WithTimestamp_SetsTimestampOnNotification()
    {
        // Arrange
        var customTimestamp = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var builder = new NotificationBuilder<TestNotification>();

        // Act
        var result = builder.WithTimestamp(customTimestamp);
        var notification = builder.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(customTimestamp, notification.Timestamp);
    }

    [Fact]
    public void ResponseBuilder_WithSuccess_SetsSuccessProperties()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponse>();

        // Act
        var result = builder.WithSuccess();
        var response = builder.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.True(response.Success);
    }

    [Fact]
    public void ResponseBuilder_WithFailure_SetsFailureProperties()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponse>();
        var errorMessage = "Custom error";

        // Act
        var result = builder.WithFailure(errorMessage);
        var response = builder.Build();

        // Assert
        Assert.Same(builder, result);
        Assert.False(response.Success);
        Assert.Equal(errorMessage, response.Message);
    }

    [Fact]
    public void ResponseBuilder_WithFailure_DefaultMessage_SetsDefaultErrorMessage()
    {
        // Arrange
        var builder = new ResponseBuilder<TestResponse>();

        // Act
        builder.WithFailure();
        var response = builder.Build();

        // Assert
        Assert.False(response.Success);
        Assert.Equal("Operation failed", response.Message);
    }


}