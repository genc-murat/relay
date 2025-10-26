using Microsoft.CodeAnalysis;
using Moq;

namespace Relay.SourceGenerator.Tests;

public class DataClassesTests
{
    [Fact]
    public void HandlerKind_EnumValues_AreDefined()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)HandlerKind.Request);
        Assert.Equal(1, (int)HandlerKind.Stream);
    }

    [Fact]
    public void PipelineScope_EnumValues_AreDefined()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)PipelineScope.All);
        Assert.Equal(1, (int)PipelineScope.Requests);
        Assert.Equal(2, (int)PipelineScope.Streams);
        Assert.Equal(3, (int)PipelineScope.Notifications);
    }

    [Fact]
    public void HandlerRegistration_ConstructorAndProperties_Work()
    {
        // Arrange
        var requestType = Mock.Of<ITypeSymbol>();
        var responseType = Mock.Of<ITypeSymbol>();
        var method = Mock.Of<IMethodSymbol>();
        var location = Location.None;

        // Act
        var registration = new HandlerRegistration
        {
            RequestType = requestType,
            ResponseType = responseType,
            Method = method,
            Name = "Test",
            Priority = 1,
            Kind = HandlerKind.Request,
            Location = location
        };

        // Assert
        Assert.Equal(requestType, registration.RequestType);
        Assert.Equal(responseType, registration.ResponseType);
        Assert.Equal(method, registration.Method);
        Assert.Equal("Test", registration.Name);
        Assert.Equal(1, registration.Priority);
        Assert.Equal(HandlerKind.Request, registration.Kind);
        Assert.Equal(location, registration.Location);
    }

    [Fact]
    public void PipelineRegistration_ConstructorAndProperties_Work()
    {
        // Arrange
        var pipelineType = Mock.Of<ITypeSymbol>();
        var method = Mock.Of<IMethodSymbol>();
        var location = Location.None;

        // Act
        var registration = new PipelineRegistration
        {
            PipelineType = pipelineType,
            Method = method,
            Order = 1,
            Scope = PipelineScope.All,
            Location = location
        };

        // Assert
        Assert.Equal(pipelineType, registration.PipelineType);
        Assert.Equal(method, registration.Method);
        Assert.Equal(1, registration.Order);
        Assert.Equal(PipelineScope.All, registration.Scope);
        Assert.Equal(location, registration.Location);
    }

    [Fact]
    public void NotificationHandlerRegistration_ConstructorAndProperties_Work()
    {
        // Arrange
        var notificationType = Mock.Of<ITypeSymbol>();
        var method = Mock.Of<IMethodSymbol>();
        var location = Location.None;

        // Act
        var registration = new NotificationHandlerRegistration
        {
            NotificationType = notificationType,
            Method = method,
            Priority = 1,
            Location = location
        };

        // Assert
        Assert.Equal(notificationType, registration.NotificationType);
        Assert.Equal(method, registration.Method);
        Assert.Equal(1, registration.Priority);
        Assert.Equal(location, registration.Location);
    }

    [Fact]
    public void HandlerInfo_ConstructorAndProperties_Work()
    {
        // Arrange
        var methodSymbol = Mock.Of<IMethodSymbol>();

        // Act
        var info = new HandlerInfo
        {
            RequestType = typeof(string),
            ResponseType = typeof(int),
            MethodSymbol = methodSymbol,
            MethodName = "Handle",
            HandlerName = "Test",
            Priority = 1,
            Attributes = new List<RelayAttributeInfo>()
        };

        // Assert
        Assert.Equal(typeof(string), info.RequestType);
        Assert.Equal(typeof(int), info.ResponseType);
        Assert.Equal(methodSymbol, info.MethodSymbol);
        Assert.Equal("Handle", info.MethodName);
        Assert.Equal("Test", info.HandlerName);
        Assert.Equal(1, info.Priority);
    }



        [Fact]
        public void RelayAttributeType_EnumValues_AreDefined()
        {
            // Arrange & Act & Assert
            Assert.Equal(0, (int)RelayAttributeType.None);
            Assert.Equal(1, (int)RelayAttributeType.Handle);
            Assert.Equal(2, (int)RelayAttributeType.Notification);
            Assert.Equal(3, (int)RelayAttributeType.Pipeline);
            Assert.Equal(4, (int)RelayAttributeType.ExposeAsEndpoint);
            Assert.Equal(5, (int)RelayAttributeType.Stream);
        }

    [Fact]
    public void HandlerInfoExtensions_HasExposeAsEndpointAttribute_WithAttribute_ReturnsTrue()
    {
        // Arrange
        var handlerInfo = new HandlerInfo
        {
            Attributes =
            [
                new() {
                    Type = RelayAttributeType.ExposeAsEndpoint,
                    AttributeData = Mock.Of<AttributeData>()
                }
            ]
        };

        // Act
        var result = handlerInfo.HasExposeAsEndpointAttribute();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HandlerInfoExtensions_HasExposeAsEndpointAttribute_WithoutAttribute_ReturnsFalse()
    {
        // Arrange
        var handlerInfo = new HandlerInfo
        {
            Attributes = new List<RelayAttributeInfo>()
        };

        // Act
        var result = handlerInfo.HasExposeAsEndpointAttribute();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HandlerInfoExtensions_GetExposeAsEndpointAttribute_WithAttribute_ReturnsAttributeData()
    {
        // Arrange
        var attributeData = Mock.Of<AttributeData>();
        var handlerInfo = new HandlerInfo
        {
            Attributes = new List<RelayAttributeInfo>
            {
                new() {
                    Type = RelayAttributeType.ExposeAsEndpoint,
                    AttributeData = attributeData
                }
            }
        };

        // Act
        var result = handlerInfo.GetExposeAsEndpointAttribute();

        // Assert
        Assert.Equal(attributeData, result);
    }

    [Fact]
    public void HandlerInfoExtensions_GetExposeAsEndpointAttribute_WithoutAttribute_ReturnsNull()
    {
        // Arrange
        var handlerInfo = new HandlerInfo
        {
            Attributes = new List<RelayAttributeInfo>()
        };

        // Act
        var result = handlerInfo.GetExposeAsEndpointAttribute();

        // Assert
        Assert.Null(result);
    }
}