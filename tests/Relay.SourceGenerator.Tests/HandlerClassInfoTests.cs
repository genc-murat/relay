using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using FluentAssertions;
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
        handlerClassInfo.ClassDeclaration.Should().NotBeNull();
        handlerClassInfo.ClassDeclaration.Should().Be(classDeclaration);
        handlerClassInfo.ClassDeclaration?.Identifier.Text.Should().Be("TestHandler");
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
        handlerClassInfo.ClassSymbol.Should().NotBeNull();
        handlerClassInfo.ClassSymbol.Should().Be(classSymbol);
        handlerClassInfo.ClassSymbol?.Name.Should().Be("TestHandler");
    }

    [Fact]
    public void HandlerClassInfo_ShouldInitializeImplementedInterfacesAsEmptyList()
    {
        // Act
        var handlerClassInfo = new HandlerClassInfo();

        // Assert
        handlerClassInfo.ImplementedInterfaces.Should().NotBeNull();
        handlerClassInfo.ImplementedInterfaces.Should().BeEmpty();
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
        handlerClassInfo.ImplementedInterfaces.Should().ContainSingle();
        handlerClassInfo.ImplementedInterfaces[0].Should().Be(interfaceInfo);
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
        handlerClassInfo.ImplementedInterfaces.Should().HaveCount(3);
        handlerClassInfo.ImplementedInterfaces.Should().Contain(interface1);
        handlerClassInfo.ImplementedInterfaces.Should().Contain(interface2);
        handlerClassInfo.ImplementedInterfaces.Should().Contain(interface3);
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
        handlerClassInfo.ClassDeclaration.Should().BeNull();
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
        handlerClassInfo.ClassSymbol.Should().BeNull();
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
        handlerClassInfo.ClassDeclaration.Should().Be(classDeclaration);
        handlerClassInfo.ClassSymbol.Should().Be(classSymbol);
        handlerClassInfo.ImplementedInterfaces.Should().ContainSingle();
        handlerClassInfo.ImplementedInterfaces[0].Should().Be(interfaceInfo);
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
        handlerClassInfo.ClassDeclaration.Should().NotBeNull();
        handlerClassInfo.ClassSymbol.Should().NotBeNull();
        handlerClassInfo.ClassSymbol?.IsGenericType.Should().BeTrue();
        handlerClassInfo.ClassSymbol?.TypeParameters.Should().HaveCount(1);
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
        handlerClassInfo.ClassDeclaration.Should().NotBeNull();
        handlerClassInfo.ClassSymbol.Should().NotBeNull();
        handlerClassInfo.ClassSymbol?.ContainingType.Should().NotBeNull();
        handlerClassInfo.ClassSymbol?.ContainingType?.Name.Should().Be("OuterClass");
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
        handlerClassInfo.ClassDeclaration.Should().NotBeNull();
        handlerClassInfo.ClassSymbol.Should().NotBeNull();
        handlerClassInfo.ClassSymbol?.IsAbstract.Should().BeTrue();
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
        handlerClassInfo.ClassDeclaration.Should().NotBeNull();
        handlerClassInfo.ClassSymbol.Should().NotBeNull();
        handlerClassInfo.ClassSymbol?.IsSealed.Should().BeTrue();
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
        handlerClassInfo.ClassDeclaration.Should().NotBeNull();
        handlerClassInfo.ClassSymbol.Should().NotBeNull();
        handlerClassInfo.ClassSymbol?.IsStatic.Should().BeTrue();
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
        handlerClassInfo.ClassDeclaration.Should().NotBeNull();
        handlerClassInfo.ClassSymbol.Should().NotBeNull();
        handlerClassInfo.ClassSymbol?.BaseType.Should().NotBeNull();
        handlerClassInfo.ClassSymbol?.BaseType?.Name.Should().Be("BaseHandler");
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
        handlerClassInfo.ImplementedInterfaces.Should().ContainSingle();
        handlerClassInfo.ImplementedInterfaces.Should().Contain(interface2);
        handlerClassInfo.ImplementedInterfaces.Should().NotContain(interface1);
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
        handlerClassInfo.ImplementedInterfaces.Should().BeEmpty();
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
