using Relay.Core.Diagnostics.Registry;
using Xunit;

namespace Relay.Core.Tests.Diagnostics.Registry;

public class HandlerInfoTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var handlerInfo = new HandlerInfo();

        // Assert
        Assert.NotNull(handlerInfo);
        Assert.IsType<HandlerInfo>(handlerInfo);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var handlerInfo = new HandlerInfo();

        // Assert
        Assert.Equal(string.Empty, handlerInfo.RequestType);
        Assert.Equal(string.Empty, handlerInfo.ResponseType);
        Assert.Equal(string.Empty, handlerInfo.HandlerType);
        Assert.Equal(string.Empty, handlerInfo.MethodName);
        Assert.Null(handlerInfo.Name);
        Assert.Equal(0, handlerInfo.Priority);
        Assert.False(handlerInfo.IsAsync);
        Assert.False(handlerInfo.IsStream);
        Assert.False(handlerInfo.IsNotification);
    }

    [Fact]
    public void RequestType_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.RequestType = "TestRequest";

        // Assert
        Assert.Equal("TestRequest", handlerInfo.RequestType);
    }

    [Fact]
    public void ResponseType_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.ResponseType = "TestResponse";

        // Assert
        Assert.Equal("TestResponse", handlerInfo.ResponseType);
    }

    [Fact]
    public void HandlerType_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.HandlerType = "TestHandler";

        // Assert
        Assert.Equal("TestHandler", handlerInfo.HandlerType);
    }

    [Fact]
    public void MethodName_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.MethodName = "HandleAsync";

        // Assert
        Assert.Equal("HandleAsync", handlerInfo.MethodName);
    }

    [Fact]
    public void Name_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.Name = "NamedHandler";

        // Assert
        Assert.Equal("NamedHandler", handlerInfo.Name);
    }

    [Fact]
    public void Name_CanBeSetToNull()
    {
        // Arrange
        var handlerInfo = new HandlerInfo
        {
            Name = "SomeName"
        };

        // Act
        handlerInfo.Name = null;

        // Assert
        Assert.Null(handlerInfo.Name);
    }

    [Fact]
    public void Priority_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.Priority = 10;

        // Assert
        Assert.Equal(10, handlerInfo.Priority);
    }

    [Fact]
    public void Priority_CanBeNegative()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.Priority = -5;

        // Assert
        Assert.Equal(-5, handlerInfo.Priority);
    }

    [Fact]
    public void IsAsync_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.IsAsync = true;

        // Assert
        Assert.True(handlerInfo.IsAsync);
    }

    [Fact]
    public void IsStream_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.IsStream = true;

        // Assert
        Assert.True(handlerInfo.IsStream);
    }

    [Fact]
    public void IsNotification_CanBeSetAndRetrieved()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.IsNotification = true;

        // Assert
        Assert.True(handlerInfo.IsNotification);
    }

    [Fact]
    public void CanBeInitializedWithObjectInitializer()
    {
        // Act
        var handlerInfo = new HandlerInfo
        {
            RequestType = "UserRequest",
            ResponseType = "UserResponse",
            HandlerType = "UserHandler",
            MethodName = "Handle",
            Name = "UserHandler",
            Priority = 5,
            IsAsync = true,
            IsStream = false,
            IsNotification = false
        };

        // Assert
        Assert.Equal("UserRequest", handlerInfo.RequestType);
        Assert.Equal("UserResponse", handlerInfo.ResponseType);
        Assert.Equal("UserHandler", handlerInfo.HandlerType);
        Assert.Equal("Handle", handlerInfo.MethodName);
        Assert.Equal("UserHandler", handlerInfo.Name);
        Assert.Equal(5, handlerInfo.Priority);
        Assert.True(handlerInfo.IsAsync);
        Assert.False(handlerInfo.IsStream);
        Assert.False(handlerInfo.IsNotification);
    }

    [Fact]
    public void MultipleInstances_HaveIndependentState()
    {
        // Arrange
        var handler1 = new HandlerInfo
        {
            RequestType = "Request1",
            HandlerType = "Handler1",
            Priority = 1
        };

        var handler2 = new HandlerInfo
        {
            RequestType = "Request2",
            HandlerType = "Handler2",
            Priority = 2
        };

        // Act & Assert
        Assert.Equal("Request1", handler1.RequestType);
        Assert.Equal("Handler1", handler1.HandlerType);
        Assert.Equal(1, handler1.Priority);

        Assert.Equal("Request2", handler2.RequestType);
        Assert.Equal("Handler2", handler2.HandlerType);
        Assert.Equal(2, handler2.Priority);
    }

    [Fact]
    public void AllProperties_CanBeSetToVariousValues()
    {
        // Arrange
        var handlerInfo = new HandlerInfo();

        // Act
        handlerInfo.RequestType = "ComplexRequest<T>";
        handlerInfo.ResponseType = "Task<ComplexResponse>";
        handlerInfo.HandlerType = "MyNamespace.MyHandler";
        handlerInfo.MethodName = "HandleComplexAsync";
        handlerInfo.Name = "complex-handler";
        handlerInfo.Priority = int.MaxValue;
        handlerInfo.IsAsync = true;
        handlerInfo.IsStream = true;
        handlerInfo.IsNotification = false;

        // Assert
        Assert.Equal("ComplexRequest<T>", handlerInfo.RequestType);
        Assert.Equal("Task<ComplexResponse>", handlerInfo.ResponseType);
        Assert.Equal("MyNamespace.MyHandler", handlerInfo.HandlerType);
        Assert.Equal("HandleComplexAsync", handlerInfo.MethodName);
        Assert.Equal("complex-handler", handlerInfo.Name);
        Assert.Equal(int.MaxValue, handlerInfo.Priority);
        Assert.True(handlerInfo.IsAsync);
        Assert.True(handlerInfo.IsStream);
        Assert.False(handlerInfo.IsNotification);
    }

    [Fact]
    public void HandlerInfo_CanRepresentNotificationHandler()
    {
        // Act
        var handlerInfo = new HandlerInfo
        {
            RequestType = "UserNotification",
            ResponseType = string.Empty, // Notifications have no response
            HandlerType = "NotificationHandler",
            MethodName = "HandleNotification",
            IsNotification = true,
            IsAsync = true,
            Priority = 0
        };

        // Assert
        Assert.Equal("UserNotification", handlerInfo.RequestType);
        Assert.Equal(string.Empty, handlerInfo.ResponseType);
        Assert.Equal("NotificationHandler", handlerInfo.HandlerType);
        Assert.Equal("HandleNotification", handlerInfo.MethodName);
        Assert.True(handlerInfo.IsNotification);
        Assert.True(handlerInfo.IsAsync);
        Assert.Equal(0, handlerInfo.Priority);
    }

    [Fact]
    public void HandlerInfo_CanRepresentStreamHandler()
    {
        // Act
        var handlerInfo = new HandlerInfo
        {
            RequestType = "StreamRequest",
            ResponseType = "IAsyncEnumerable<StreamResponse>",
            HandlerType = "StreamHandler",
            MethodName = "HandleStream",
            IsStream = true,
            IsAsync = true,
            Priority = 10
        };

        // Assert
        Assert.Equal("StreamRequest", handlerInfo.RequestType);
        Assert.Equal("IAsyncEnumerable<StreamResponse>", handlerInfo.ResponseType);
        Assert.Equal("StreamHandler", handlerInfo.HandlerType);
        Assert.Equal("HandleStream", handlerInfo.MethodName);
        Assert.True(handlerInfo.IsStream);
        Assert.True(handlerInfo.IsAsync);
        Assert.Equal(10, handlerInfo.Priority);
    }
}