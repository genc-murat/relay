using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using System.Linq;

#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8604 // Possible null reference argument
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for HandlerClassInfo value-based equality implementation.
/// Ensures proper incremental generator caching behavior.
/// </summary>
public class HandlerClassInfoEqualityTests
{
    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var handlerInfo = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var result = handlerInfo.Equals(handlerInfo);

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
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var handlerInfo = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var result = handlerInfo.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_SameClassSymbol_NoInterfaces_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        
        var handlerInfo1 = new HandlerClassInfo { ClassSymbol = classSymbol };
        var handlerInfo2 = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var result = handlerInfo1.Equals(handlerInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_DifferentClassSymbol_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler1 { }
    public class TestHandler2 { }
}");
        var classSymbol1 = compilation.GetTypeByMetadataName("TestApp.TestHandler1");
        var classSymbol2 = compilation.GetTypeByMetadataName("TestApp.TestHandler2");
        
        var handlerInfo1 = new HandlerClassInfo { ClassSymbol = classSymbol1 };
        var handlerInfo2 = new HandlerClassInfo { ClassSymbol = classSymbol2 };

        // Act
        var result = handlerInfo1.Equals(handlerInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_SameClassSymbol_SameInterfaces_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var handlerInfo1 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
                }
            }
        };
        
        var handlerInfo2 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
                }
            }
        };

        // Act
        var result = handlerInfo1.Equals(handlerInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_SameClassSymbol_DifferentInterfaceCount_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var handlerInfo1 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
                }
            }
        };
        
        var handlerInfo2 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
        };

        // Act
        var result = handlerInfo1.Equals(handlerInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_SameClassSymbol_DifferentInterfaces_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public interface INotification { }
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var requestSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var notificationSymbol = compilation.GetTypeByMetadataName("TestApp.INotification");
        
        var handlerInfo1 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)requestSymbol
                }
            }
        };
        
        var handlerInfo2 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Notification,
                    InterfaceSymbol = (INamedTypeSymbol)notificationSymbol
                }
            }
        };

        // Act
        var result = handlerInfo1.Equals(handlerInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_MultipleInterfaces_SameOrder_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public interface INotification { }
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var requestSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var notificationSymbol = compilation.GetTypeByMetadataName("TestApp.INotification");
        
        var handlerInfo1 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)requestSymbol
                },
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Notification,
                    InterfaceSymbol = (INamedTypeSymbol)notificationSymbol
                }
            }
        };
        
        var handlerInfo2 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)requestSymbol
                },
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Notification,
                    InterfaceSymbol = (INamedTypeSymbol)notificationSymbol
                }
            }
        };

        // Act
        var result = handlerInfo1.Equals(handlerInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_MultipleInterfaces_DifferentOrder_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public interface INotification { }
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var requestSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        var notificationSymbol = compilation.GetTypeByMetadataName("TestApp.INotification");
        
        var handlerInfo1 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)requestSymbol
                },
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Notification,
                    InterfaceSymbol = (INamedTypeSymbol)notificationSymbol
                }
            }
        };
        
        var handlerInfo2 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Notification,
                    InterfaceSymbol = (INamedTypeSymbol)notificationSymbol
                },
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)requestSymbol
                }
            }
        };

        // Act
        var result = handlerInfo1.Equals(handlerInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHashCode_SameClassSymbol_NoInterfaces_ReturnsSameHash()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        
        var handlerInfo1 = new HandlerClassInfo { ClassSymbol = classSymbol };
        var handlerInfo2 = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var hash1 = handlerInfo1.GetHashCode();
        var hash2 = handlerInfo2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_SameClassSymbol_SameInterfaces_ReturnsSameHash()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public interface IRequest { }
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var interfaceSymbol = compilation.GetTypeByMetadataName("TestApp.IRequest");
        
        var handlerInfo1 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
                }
            }
        };
        
        var handlerInfo2 = new HandlerClassInfo 
        { 
            ClassSymbol = classSymbol,
            ImplementedInterfaces = new()
            {
                new HandlerInterfaceInfo 
                { 
                    InterfaceType = HandlerType.Request,
                    InterfaceSymbol = (INamedTypeSymbol)interfaceSymbol
                }
            }
        };

        // Act
        var hash1 = handlerInfo1.GetHashCode();
        var hash2 = handlerInfo2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentClassSymbol_ReturnsDifferentHash()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler1 { }
    public class TestHandler2 { }
}");
        var classSymbol1 = compilation.GetTypeByMetadataName("TestApp.TestHandler1");
        var classSymbol2 = compilation.GetTypeByMetadataName("TestApp.TestHandler2");
        
        var handlerInfo1 = new HandlerClassInfo { ClassSymbol = classSymbol1 };
        var handlerInfo2 = new HandlerClassInfo { ClassSymbol = classSymbol2 };

        // Act
        var hash1 = handlerInfo1.GetHashCode();
        var hash2 = handlerInfo2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void EqualsObject_WithHandlerClassInfo_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        
        var handlerInfo1 = new HandlerClassInfo { ClassSymbol = classSymbol };
        object handlerInfo2 = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var result = handlerInfo1.Equals(handlerInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EqualsObject_WithNonHandlerClassInfo_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        
        var handlerInfo = new HandlerClassInfo { ClassSymbol = classSymbol };
        object other = "not a HandlerClassInfo";

        // Act
        var result = handlerInfo.Equals(other);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsObject_WithNull_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        
        var handlerInfo = new HandlerClassInfo { ClassSymbol = classSymbol };
        object other = null;

        // Act
        var result = handlerInfo.Equals(other);

        // Assert
        Assert.False(result);
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
