using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

#pragma warning disable CS8604 // Possible null reference argument

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for HandlerClassInfoComparer singleton comparer.
/// Ensures proper incremental generator pipeline caching.
/// </summary>
public class HandlerClassInfoComparerTests
{
    [Fact]
    public void Instance_IsSingleton()
    {
        // Act
        var instance1 = HandlerClassInfoComparer.Instance;
        var instance2 = HandlerClassInfoComparer.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;

        // Act
        var result = comparer.Equals(null, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_FirstNull_ReturnsFalse()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var handlerInfo = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var result = comparer.Equals(null, handlerInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_SecondNull_ReturnsFalse()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var handlerInfo = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var result = comparer.Equals(handlerInfo, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var handlerInfo = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var result = comparer.Equals(handlerInfo, handlerInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_EqualHandlerInfos_ReturnsTrue()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        
        var handlerInfo1 = new HandlerClassInfo { ClassSymbol = classSymbol };
        var handlerInfo2 = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var result = comparer.Equals(handlerInfo1, handlerInfo2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_DifferentHandlerInfos_ReturnsFalse()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
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
        var result = comparer.Equals(handlerInfo1, handlerInfo2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHashCode_Null_ReturnsZero()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;

        // Act
        var hash = comparer.GetHashCode(null);

        // Assert
        Assert.Equal(0, hash);
    }

    [Fact]
    public void GetHashCode_NonNull_ReturnsNonZero()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        var handlerInfo = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var hash = comparer.GetHashCode(handlerInfo);

        // Assert
        Assert.NotEqual(0, hash);
    }

    [Fact]
    public void GetHashCode_EqualHandlerInfos_ReturnsSameHash()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        
        var handlerInfo1 = new HandlerClassInfo { ClassSymbol = classSymbol };
        var handlerInfo2 = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var hash1 = comparer.GetHashCode(handlerInfo1);
        var hash2 = comparer.GetHashCode(handlerInfo2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentHandlerInfos_ReturnsDifferentHash()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
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
        var hash1 = comparer.GetHashCode(handlerInfo1);
        var hash2 = comparer.GetHashCode(handlerInfo2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Comparer_WorksWithDictionary()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
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
        var handlerInfo1Copy = new HandlerClassInfo { ClassSymbol = classSymbol1 };

        var dictionary = new System.Collections.Generic.Dictionary<HandlerClassInfo, string>(comparer)
        {
            { handlerInfo1, "Handler1" },
            { handlerInfo2, "Handler2" }
        };

        // Act & Assert
        Assert.True(dictionary.ContainsKey(handlerInfo1Copy));
        Assert.Equal("Handler1", dictionary[handlerInfo1Copy]);
        Assert.Equal(2, dictionary.Count);
    }

    [Fact]
    public void Comparer_WorksWithHashSet()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
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
        var handlerInfo1Copy = new HandlerClassInfo { ClassSymbol = classSymbol1 };

        var hashSet = new System.Collections.Generic.HashSet<HandlerClassInfo>(comparer)
        {
            handlerInfo1,
            handlerInfo2
        };

        // Act
        var addedDuplicate = hashSet.Add(handlerInfo1Copy);

        // Assert
        Assert.False(addedDuplicate); // Should not add duplicate
        Assert.Equal(2, hashSet.Count);
        Assert.Contains(handlerInfo1Copy, hashSet);
    }

    [Fact]
    public void Comparer_ConsistentWithEqualsAndGetHashCode()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");
        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");
        
        var handlerInfo1 = new HandlerClassInfo { ClassSymbol = classSymbol };
        var handlerInfo2 = new HandlerClassInfo { ClassSymbol = classSymbol };

        // Act
        var equals = comparer.Equals(handlerInfo1, handlerInfo2);
        var hash1 = comparer.GetHashCode(handlerInfo1);
        var hash2 = comparer.GetHashCode(handlerInfo2);

        // Assert - If equals returns true, hash codes must be equal
        Assert.True(equals);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Comparer_WithInterfaces_WorksCorrectly()
    {
        // Arrange
        var comparer = HandlerClassInfoComparer.Instance;
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
        var equals = comparer.Equals(handlerInfo1, handlerInfo2);
        var hash1 = comparer.GetHashCode(handlerInfo1);
        var hash2 = comparer.GetHashCode(handlerInfo2);

        // Assert
        Assert.True(equals);
        Assert.Equal(hash1, hash2);
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
