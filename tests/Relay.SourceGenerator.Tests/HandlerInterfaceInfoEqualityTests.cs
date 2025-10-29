using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8604 // Possible null reference argument

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for HandlerInterfaceInfo value-based equality implementation.
/// Ensures proper incremental generator caching behavior.
/// </summary>
public class HandlerInterfaceInfoEqualityTests
{
    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var interfaceInfo = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };

        // Act
        var result = interfaceInfo.Equals(interfaceInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_NullOther_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var interfaceInfo = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };

        // Act
        var result = interfaceInfo.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_SameProperties_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class RequestType { }
    public class ResponseType { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var requestType = compilation.GetTypeByMetadataName("TestApp.RequestType");
        var responseType = compilation.GetTypeByMetadataName("TestApp.ResponseType");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = requestType,
            ResponseType = responseType
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = requestType,
            ResponseType = responseType
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_DifferentInterfaceType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Notification,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_DifferentInterfaceSymbol_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest1 { }
    public interface IRequest2 { }
}");
        var interfaceSymbol1 = compilation.GetTypeByMetadataName("TestApp.IRequest1");
        var interfaceSymbol2 = compilation.GetTypeByMetadataName("TestApp.IRequest2");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol1
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol2
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_DifferentRequestType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class RequestType1 { }
    public class RequestType2 { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var requestType1 = compilation.GetTypeByMetadataName("TestApp.RequestType1");
        var requestType2 = compilation.GetTypeByMetadataName("TestApp.RequestType2");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = requestType1
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = requestType2
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_DifferentResponseType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class ResponseType1 { }
    public class ResponseType2 { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var responseType1 = compilation.GetTypeByMetadataName("TestApp.ResponseType1");
        var responseType2 = compilation.GetTypeByMetadataName("TestApp.ResponseType2");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            ResponseType = responseType1
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            ResponseType = responseType2
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_OneNullRequestType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class RequestType { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var requestType = compilation.GetTypeByMetadataName("TestApp.RequestType");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = requestType
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = null
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_BothNullRequestType_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = null
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = null
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_OneNullResponseType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class ResponseType { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var responseType = compilation.GetTypeByMetadataName("TestApp.ResponseType");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            ResponseType = responseType
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            ResponseType = null
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_BothNullResponseType_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            ResponseType = null
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            ResponseType = null
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetHashCode_SameProperties_ReturnsSameHash()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class RequestType { }
    public class ResponseType { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var requestType = compilation.GetTypeByMetadataName("TestApp.RequestType");
        var responseType = compilation.GetTypeByMetadataName("TestApp.ResponseType");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = requestType,
            ResponseType = responseType
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = requestType,
            ResponseType = responseType
        };

        // Act
        var hash1 = interfaceInfo1.GetHashCode();
        var hash2 = interfaceInfo2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentInterfaceType_ReturnsDifferentHash()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };
        
        var interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Notification,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };

        // Act
        var hash1 = interfaceInfo1.GetHashCode();
        var hash2 = interfaceInfo2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_NullRequestType_DoesNotThrow()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var interfaceInfo = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            RequestType = null
        };

        // Act & Assert
        var hash = interfaceInfo.GetHashCode();
        Assert.NotEqual(0, hash); // Should produce a valid hash
    }

    [Fact]
    public void GetHashCode_NullResponseType_DoesNotThrow()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var interfaceInfo = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol,
            ResponseType = null
        };

        // Act & Assert
        var hash = interfaceInfo.GetHashCode();
        Assert.NotEqual(0, hash); // Should produce a valid hash
    }

    [Fact]
    public void EqualsObject_WithHandlerInterfaceInfo_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var interfaceInfo1 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };
        object interfaceInfo2 = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };

        // Act
        var result = interfaceInfo1.Equals(interfaceInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EqualsObject_WithNonHandlerInterfaceInfo_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var interfaceInfo = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };
        object other = "not a HandlerInterfaceInfo";

        // Act
        var result = interfaceInfo.Equals(other);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_AllHandlerTypes_WorkCorrectly()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
}");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var requestInfo = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Request,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };
        
        var notificationInfo = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Notification,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };
        
        var streamInfo = new HandlerInterfaceInfo 
        { 
            InterfaceType = HandlerType.Stream,
            InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
        };

        // Act & Assert
        Assert.False(requestInfo.Equals(notificationInfo));
        Assert.False(requestInfo.Equals(streamInfo));
        Assert.False(notificationInfo.Equals(streamInfo));
    }

    private Compilation CreateTestCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
