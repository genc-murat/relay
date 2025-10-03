using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for HandlerDiscoveryResult and related data structures
/// </summary>
public class HandlerDiscoveryResultTests
{
    [Fact]
    public void HandlerDiscoveryResult_InitializesEmptyCollections()
    {
        // Act
        var result = new HandlerDiscoveryResult();

        // Assert
        Assert.NotNull(result.Handlers);
        Assert.NotNull(result.NotificationHandlers);
        Assert.NotNull(result.PipelineBehaviors);
        Assert.NotNull(result.StreamHandlers);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
    }

    [Fact]
    public void HandlerDiscoveryResult_CanAddHandlers()
    {
        // Arrange
        var result = new HandlerDiscoveryResult();
        var handler = new HandlerInfo
        {
            MethodName = "HandleAsync",
            HandlerName = "MyHandler",
            Priority = 1,
            IsAsync = true
        };

        // Act
        result.Handlers.Add(handler);

        // Assert
        Assert.Single(result.Handlers);
        Assert.Equal("HandleAsync", result.Handlers[0].MethodName);
    }

    [Fact]
    public void HandlerInfo_DefaultValues()
    {
        // Act
        var handler = new HandlerInfo();

        // Assert
        Assert.Null(handler.HandlerType);
        Assert.Null(handler.RequestType);
        Assert.Null(handler.ResponseType);
        Assert.Null(handler.MethodName);
        Assert.Null(handler.HandlerName);
        Assert.Equal(0, handler.Priority);
        Assert.False(handler.IsAsync);
        Assert.False(handler.HasCancellationToken);
        Assert.NotNull(handler.Attributes);
        Assert.Empty(handler.Attributes);
    }

    [Fact]
    public void HandlerInfo_CanSetAllProperties()
    {
        // Act
        var handler = new HandlerInfo
        {
            MethodName = "HandleAsync",
            HandlerName = "UserHandler",
            Priority = 10,
            IsAsync = true,
            HasCancellationToken = true,
            FullTypeName = "MyApp.Handlers.UserHandler",
            Namespace = "MyApp.Handlers"
        };

        // Assert
        Assert.Equal("HandleAsync", handler.MethodName);
        Assert.Equal("UserHandler", handler.HandlerName);
        Assert.Equal(10, handler.Priority);
        Assert.True(handler.IsAsync);
        Assert.True(handler.HasCancellationToken);
        Assert.Equal("MyApp.Handlers.UserHandler", handler.FullTypeName);
        Assert.Equal("MyApp.Handlers", handler.Namespace);
    }

    [Fact]
    public void NotificationHandlerInfo_DefaultValues()
    {
        // Act
        var handler = new NotificationHandlerInfo();

        // Assert
        Assert.Null(handler.HandlerType);
        Assert.Null(handler.NotificationType);
        Assert.Null(handler.MethodName);
        Assert.Equal(0, handler.Priority);
        Assert.False(handler.IsAsync);
        Assert.False(handler.HasCancellationToken);
        Assert.Null(handler.DispatchMode);
    }

    [Fact]
    public void NotificationHandlerInfo_CanSetAllProperties()
    {
        // Act
        var handler = new NotificationHandlerInfo
        {
            MethodName = "HandleAsync",
            Priority = 5,
            IsAsync = true,
            HasCancellationToken = true,
            DispatchMode = "Parallel",
            FullTypeName = "MyApp.Handlers.NotificationHandler",
            Namespace = "MyApp.Handlers"
        };

        // Assert
        Assert.Equal("HandleAsync", handler.MethodName);
        Assert.Equal(5, handler.Priority);
        Assert.True(handler.IsAsync);
        Assert.True(handler.HasCancellationToken);
        Assert.Equal("Parallel", handler.DispatchMode);
    }

    [Fact]
    public void PipelineBehaviorInfo_DefaultValues()
    {
        // Act
        var behavior = new PipelineBehaviorInfo();

        // Assert
        Assert.Null(behavior.BehaviorType);
        Assert.Null(behavior.RequestType);
        Assert.Null(behavior.ResponseType);
        Assert.Null(behavior.MethodName);
        Assert.Equal(0, behavior.Order);
        Assert.Null(behavior.Scope);
        Assert.False(behavior.IsAsync);
    }

    [Fact]
    public void PipelineBehaviorInfo_CanSetAllProperties()
    {
        // Act
        var behavior = new PipelineBehaviorInfo
        {
            MethodName = "HandleAsync",
            Order = 100,
            Scope = "Global",
            IsAsync = true,
            FullTypeName = "MyApp.Pipeline.LoggingBehavior",
            Namespace = "MyApp.Pipeline"
        };

        // Assert
        Assert.Equal("HandleAsync", behavior.MethodName);
        Assert.Equal(100, behavior.Order);
        Assert.Equal("Global", behavior.Scope);
        Assert.True(behavior.IsAsync);
    }

    [Fact]
    public void StreamHandlerInfo_DefaultValues()
    {
        // Act
        var handler = new StreamHandlerInfo();

        // Assert
        Assert.Null(handler.HandlerType);
        Assert.Null(handler.RequestType);
        Assert.Null(handler.ResponseType);
        Assert.Null(handler.MethodName);
        Assert.Null(handler.HandlerName);
        Assert.Equal(0, handler.Priority);
        Assert.False(handler.IsAsync);
        Assert.False(handler.HasCancellationToken);
    }

    [Fact]
    public void StreamHandlerInfo_CanSetAllProperties()
    {
        // Act
        var handler = new StreamHandlerInfo
        {
            MethodName = "HandleAsync",
            HandlerName = "DataStreamHandler",
            Priority = 3,
            IsAsync = true,
            HasCancellationToken = true,
            FullTypeName = "MyApp.Handlers.DataStreamHandler",
            Namespace = "MyApp.Handlers"
        };

        // Assert
        Assert.Equal("HandleAsync", handler.MethodName);
        Assert.Equal("DataStreamHandler", handler.HandlerName);
        Assert.Equal(3, handler.Priority);
        Assert.True(handler.IsAsync);
        Assert.True(handler.HasCancellationToken);
    }

    [Fact]
    public void RelayAttributeInfo_DefaultValues()
    {
        // Act
        var attrInfo = new RelayAttributeInfo();

        // Assert
        Assert.Equal(RelayAttributeType.None, attrInfo.Type);
        Assert.Null(attrInfo.AttributeData);
    }

    [Fact]
    public void RelayAttributeInfo_CanSetProperties()
    {
        // Act
        var attrInfo = new RelayAttributeInfo
        {
            Type = RelayAttributeType.Handle
        };

        // Assert
        Assert.Equal(RelayAttributeType.Handle, attrInfo.Type);
    }

    [Theory]
    [InlineData(RelayAttributeType.None)]
    [InlineData(RelayAttributeType.Handle)]
    [InlineData(RelayAttributeType.Notification)]
    [InlineData(RelayAttributeType.Pipeline)]
    [InlineData(RelayAttributeType.ExposeAsEndpoint)]
    [InlineData(RelayAttributeType.Stream)]
    public void RelayAttributeType_AllEnumValues_CanBeSet(RelayAttributeType attributeType)
    {
        // Act
        var attrInfo = new RelayAttributeInfo { Type = attributeType };

        // Assert
        Assert.Equal(attributeType, attrInfo.Type);
    }

    [Fact]
    public void HandlerInfo_AttributesList_IsModifiable()
    {
        // Arrange
        var handler = new HandlerInfo();
        var attr1 = new RelayAttributeInfo { Type = RelayAttributeType.Handle };
        var attr2 = new RelayAttributeInfo { Type = RelayAttributeType.ExposeAsEndpoint };

        // Act
        handler.Attributes.Add(attr1);
        handler.Attributes.Add(attr2);

        // Assert
        Assert.Equal(2, handler.Attributes.Count);
        Assert.Contains(attr1, handler.Attributes);
        Assert.Contains(attr2, handler.Attributes);
    }

    [Fact]
    public void HandlerDiscoveryResult_CanAddMultipleHandlerTypes()
    {
        // Arrange
        var result = new HandlerDiscoveryResult();

        // Act
        result.Handlers.Add(new HandlerInfo { MethodName = "Handler1" });
        result.NotificationHandlers.Add(new NotificationHandlerInfo { MethodName = "NotificationHandler1" });
        result.PipelineBehaviors.Add(new PipelineBehaviorInfo { MethodName = "Behavior1" });
        result.StreamHandlers.Add(new StreamHandlerInfo { MethodName = "StreamHandler1" });

        // Assert
        Assert.Single(result.Handlers);
        Assert.Single(result.NotificationHandlers);
        Assert.Single(result.PipelineBehaviors);
        Assert.Single(result.StreamHandlers);
    }
}
