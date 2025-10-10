using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using System.Linq;

namespace Relay.SourceGenerator.Tests;

public class HandlerClassInfoTests
{
    [Fact]
    public void HandlerClassInfo_ShouldSetClassDeclaration()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class TestHandler { }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var classDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration
        };

        // Assert
        Assert.NotNull(handlerClassInfo.ClassDeclaration);
        Assert.Equal(classDeclaration, handlerClassInfo.ClassDeclaration);
        Assert.Equal("TestHandler", handlerClassInfo.ClassDeclaration?.Identifier.Text);
    }

    [Fact]
    public void HandlerClassInfo_ShouldSetClassSymbol()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
namespace TestApp
{
    public class TestHandler { }
}");

        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassSymbol = classSymbol
        };

        // Assert
        Assert.NotNull(handlerClassInfo.ClassSymbol);
        Assert.Equal(classSymbol, handlerClassInfo.ClassSymbol);
        Assert.Equal("TestHandler", handlerClassInfo.ClassSymbol?.Name);
    }

    [Fact]
    public void HandlerClassInfo_ShouldInitializeImplementedInterfacesAsEmptyList()
    {
        // Act
        var handlerClassInfo = new HandlerClassInfo();

        // Assert
        Assert.NotNull(handlerClassInfo.ImplementedInterfaces);
        Assert.Empty(handlerClassInfo.ImplementedInterfaces);
    }

    [Fact]
    public void HandlerClassInfo_ShouldAllowAddingImplementedInterfaces()
    {
        // Arrange
        var handlerClassInfo = new HandlerClassInfo();
        var interfaceInfo = new HandlerInterfaceInfo
        {
            InterfaceType = HandlerType.Request
        };

        // Act
        handlerClassInfo.ImplementedInterfaces.Add(interfaceInfo);

        // Assert
        Assert.Single(handlerClassInfo.ImplementedInterfaces);
        Assert.Equal(interfaceInfo, handlerClassInfo.ImplementedInterfaces[0]);
    }

    [Fact]
    public void HandlerClassInfo_ShouldAllowMultipleImplementedInterfaces()
    {
        // Arrange
        var handlerClassInfo = new HandlerClassInfo();
        var interface1 = new HandlerInterfaceInfo { InterfaceType = HandlerType.Request };
        var interface2 = new HandlerInterfaceInfo { InterfaceType = HandlerType.Notification };
        var interface3 = new HandlerInterfaceInfo { InterfaceType = HandlerType.Stream };

        // Act
        handlerClassInfo.ImplementedInterfaces.Add(interface1);
        handlerClassInfo.ImplementedInterfaces.Add(interface2);
        handlerClassInfo.ImplementedInterfaces.Add(interface3);

        // Assert
        Assert.Equal(3, handlerClassInfo.ImplementedInterfaces.Count);
        Assert.Contains(interface1, handlerClassInfo.ImplementedInterfaces);
        Assert.Contains(interface2, handlerClassInfo.ImplementedInterfaces);
        Assert.Contains(interface3, handlerClassInfo.ImplementedInterfaces);
    }

    [Fact]
    public void HandlerClassInfo_ShouldAllowNullClassDeclaration()
    {
        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = null
        };

        // Assert
        Assert.Null(handlerClassInfo.ClassDeclaration);
    }

    [Fact]
    public void HandlerClassInfo_ShouldAllowNullClassSymbol()
    {
        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassSymbol = null
        };

        // Assert
        Assert.Null(handlerClassInfo.ClassSymbol);
    }

    [Fact]
    public void HandlerClassInfo_ShouldSetAllProperties()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class TestHandler { }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var classDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler");

        var interfaceInfo = new HandlerInterfaceInfo
        {
            InterfaceType = HandlerType.Request
        };

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol
        };
        handlerClassInfo.ImplementedInterfaces.Add(interfaceInfo);

        // Assert
        Assert.Equal(classDeclaration, handlerClassInfo.ClassDeclaration);
        Assert.Equal(classSymbol, handlerClassInfo.ClassSymbol);
        Assert.Single(handlerClassInfo.ImplementedInterfaces);
        Assert.Equal(interfaceInfo, handlerClassInfo.ImplementedInterfaces[0]);
    }

    [Fact]
    public void HandlerClassInfo_ShouldHandleGenericClass()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class TestHandler<T> { }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var classDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        var classSymbol = compilation.GetTypeByMetadataName("TestApp.TestHandler`1");

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol
        };

        // Assert
        Assert.NotNull(handlerClassInfo.ClassDeclaration);
        Assert.NotNull(handlerClassInfo.ClassSymbol);
        Assert.True(handlerClassInfo.ClassSymbol?.IsGenericType);
        Assert.Single(handlerClassInfo.ClassSymbol?.TypeParameters);
    }

    [Fact]
    public void HandlerClassInfo_ShouldHandleNestedClass()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class OuterClass
    {
        public class InnerHandler { }
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var classDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == "InnerHandler");

        var classSymbol = compilation.GetTypeByMetadataName("TestApp.OuterClass+InnerHandler");

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol
        };

        // Assert
        Assert.NotNull(handlerClassInfo.ClassDeclaration);
        Assert.NotNull(handlerClassInfo.ClassSymbol);
        Assert.NotNull(handlerClassInfo.ClassSymbol?.ContainingType);
        Assert.Equal("OuterClass", handlerClassInfo.ClassSymbol?.ContainingType?.Name);
    }

    [Fact]
    public void HandlerClassInfo_ShouldHandleAbstractClass()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public abstract class AbstractHandler { }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var classDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        var classSymbol = compilation.GetTypeByMetadataName("TestApp.AbstractHandler");

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol
        };

        // Assert
        Assert.NotNull(handlerClassInfo.ClassDeclaration);
        Assert.NotNull(handlerClassInfo.ClassSymbol);
        Assert.True(handlerClassInfo.ClassSymbol?.IsAbstract);
    }

    [Fact]
    public void HandlerClassInfo_ShouldHandleSealedClass()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public sealed class SealedHandler { }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var classDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        var classSymbol = compilation.GetTypeByMetadataName("TestApp.SealedHandler");

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol
        };

        // Assert
        Assert.NotNull(handlerClassInfo.ClassDeclaration);
        Assert.NotNull(handlerClassInfo.ClassSymbol);
        Assert.True(handlerClassInfo.ClassSymbol?.IsSealed);
    }

    [Fact]
    public void HandlerClassInfo_ShouldHandleStaticClass()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public static class StaticHandler { }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var classDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        var classSymbol = compilation.GetTypeByMetadataName("TestApp.StaticHandler");

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol
        };

        // Assert
        Assert.NotNull(handlerClassInfo.ClassDeclaration);
        Assert.NotNull(handlerClassInfo.ClassSymbol);
        Assert.True(handlerClassInfo.ClassSymbol?.IsStatic);
    }

    [Fact]
    public void HandlerClassInfo_ShouldHandleClassWithBaseType()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class BaseHandler { }
    public class DerivedHandler : BaseHandler { }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var classDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == "DerivedHandler");

        var classSymbol = compilation.GetTypeByMetadataName("TestApp.DerivedHandler");

        // Act
        var handlerClassInfo = new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol
        };

        // Assert
        Assert.NotNull(handlerClassInfo.ClassDeclaration);
        Assert.NotNull(handlerClassInfo.ClassSymbol);
        Assert.NotNull(handlerClassInfo.ClassSymbol?.BaseType);
        Assert.Equal("BaseHandler", handlerClassInfo.ClassSymbol?.BaseType?.Name);
    }

    [Fact]
    public void HandlerClassInfo_ImplementedInterfaces_ShouldBeModifiable()
    {
        // Arrange
        var handlerClassInfo = new HandlerClassInfo();
        var interface1 = new HandlerInterfaceInfo { InterfaceType = HandlerType.Request };
        var interface2 = new HandlerInterfaceInfo { InterfaceType = HandlerType.Notification };

        // Act
        handlerClassInfo.ImplementedInterfaces.Add(interface1);
        handlerClassInfo.ImplementedInterfaces.Add(interface2);
        handlerClassInfo.ImplementedInterfaces.Remove(interface1);

        // Assert
        Assert.Single(handlerClassInfo.ImplementedInterfaces);
        Assert.Contains(interface2, handlerClassInfo.ImplementedInterfaces);
        Assert.DoesNotContain(interface1, handlerClassInfo.ImplementedInterfaces);
    }

    [Fact]
    public void HandlerClassInfo_ImplementedInterfaces_ShouldSupportClear()
    {
        // Arrange
        var handlerClassInfo = new HandlerClassInfo();
        handlerClassInfo.ImplementedInterfaces.Add(new HandlerInterfaceInfo { InterfaceType = HandlerType.Request });
        handlerClassInfo.ImplementedInterfaces.Add(new HandlerInterfaceInfo { InterfaceType = HandlerType.Notification });

        // Act
        handlerClassInfo.ImplementedInterfaces.Clear();

        // Assert
        Assert.Empty(handlerClassInfo.ImplementedInterfaces);
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
