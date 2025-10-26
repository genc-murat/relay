extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Validators;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for the TypeValidator class methods.
/// </summary>
public class TypeValidatorTests
{
    /// <summary>
    /// Tests that IsValidVoidHandlerReturnType returns true for Task return type.
    /// </summary>
    [Fact]
    public void IsValidVoidHandlerReturnType_Task_ReturnsTrue()
    {
        // Setup compilation with test code to extract the Task type
        var sourceCode = @"
using System.Threading.Tasks;

namespace TestNamespace 
{
    public class TestHelper 
    { 
        public Task GetTask() => null;
    } 
}";

        var compilation = CSharpCompilation.Create("test")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Task).Assembly.Location));

        var helperClass = compilation.GetTypeByMetadataName("TestNamespace.TestHelper");
        var method = (IMethodSymbol)helperClass.GetMembers("GetTask").First();
        var returnType = method.ReturnType;

        // Act
        var result = TypeValidator.IsValidVoidHandlerReturnType(returnType);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidVoidHandlerReturnType returns true for ValueTask return type.
    /// </summary>
    [Fact]
    public void IsValidVoidHandlerReturnType_ValueTask_ReturnsTrue()
    {
        // Setup compilation with test code to extract the ValueTask type
        var sourceCode = @"
using System.Threading.Tasks;

namespace TestNamespace 
{
    public class TestHelper 
    { 
        public ValueTask GetValueTask() => default;
    } 
}";

        var compilation = CSharpCompilation.Create("test")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location));

        var helperClass = compilation.GetTypeByMetadataName("TestNamespace.TestHelper");
        var method = (IMethodSymbol)helperClass.GetMembers("GetValueTask").First();
        var returnType = method.ReturnType;

        // Act
        var result = TypeValidator.IsValidVoidHandlerReturnType(returnType);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidVoidHandlerReturnType returns false for Task<T> return type.
    /// </summary>
    [Fact]
    public void IsValidVoidHandlerReturnType_TaskOfT_ReturnsFalse()
    {
        // Setup compilation with test code to extract the Task<T> type
        var sourceCode = @"
using System.Threading.Tasks;

namespace TestNamespace 
{
    public class TestHelper 
    { 
        public Task<string> GetTaskOfString() => null;
    } 
}";

        var compilation = CSharpCompilation.Create("test")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location));

        var helperClass = compilation.GetTypeByMetadataName("TestNamespace.TestHelper");
        var method = (IMethodSymbol)helperClass.GetMembers("GetTaskOfString").First();
        var returnType = method.ReturnType;

        // Act
        var result = TypeValidator.IsValidVoidHandlerReturnType(returnType);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsValidVoidHandlerReturnType returns false for ValueTask<T> return type.
    /// </summary>
    [Fact]
    public void IsValidVoidHandlerReturnType_ValueTaskOfT_ReturnsFalse()
    {
        // Setup compilation with test code to extract the ValueTask<T> type
        var sourceCode = @"
using System.Threading.Tasks;

namespace TestNamespace 
{
    public class TestHelper 
    { 
        public ValueTask<string> GetValueTaskOfString() => default;
    } 
}";

        var compilation = CSharpCompilation.Create("test")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ValueTask<>).Assembly.Location));

        var helperClass = compilation.GetTypeByMetadataName("TestNamespace.TestHelper");
        var method = (IMethodSymbol)helperClass.GetMembers("GetValueTaskOfString").First();
        var returnType = method.ReturnType;

        // Act
        var result = TypeValidator.IsValidVoidHandlerReturnType(returnType);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsValidVoidHandlerReturnType returns false for other types (e.g., string).
    /// </summary>
    [Fact]
    public void IsValidVoidHandlerReturnType_OtherTypes_ReturnsFalse()
    {
        // Setup compilation with test code to extract a non-matching type
        var sourceCode = @"
namespace TestNamespace 
{
    public class TestHelper 
    { 
        public string GetString() => null;
    } 
}";

        var compilation = CSharpCompilation.Create("test")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var helperClass = compilation.GetTypeByMetadataName("TestNamespace.TestHelper");
        var method = (IMethodSymbol)helperClass.GetMembers("GetString").First();
        var returnType = method.ReturnType;

        // Act
        var result = TypeValidator.IsValidVoidHandlerReturnType(returnType);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsValidVoidHandlerReturnType returns false for void return type.
    /// </summary>
    [Fact]
    public void IsValidVoidHandlerReturnType_Void_ReturnsFalse()
    {
        // Setup compilation with test code to extract a void type
        var sourceCode = @"
namespace TestNamespace 
{
    public class TestHelper 
    { 
        public void GetVoid() { }
    } 
}";

        var compilation = CSharpCompilation.Create("test")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var helperClass = compilation.GetTypeByMetadataName("TestNamespace.TestHelper");
        var method = (IMethodSymbol)helperClass.GetMembers("GetVoid").First();
        var returnType = method.ReturnType;

        // Act
        var result = TypeValidator.IsValidVoidHandlerReturnType(returnType);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsValidVoidHandlerReturnType returns true when the return type is Task without being an INamedTypeSymbol.
    /// This tests the fallback condition in the method.
    /// </summary>
    [Fact]
    public void IsValidVoidHandlerReturnType_TaskNameOnly_ReturnsTrue()
    {
        // Setup compilation to get Task type
        var sourceCode = @"
using System.Threading.Tasks;

namespace TestNamespace 
{
    public class TestHelper 
    { 
        public Task GetTask() => null;
    } 
}";

        var compilation = CSharpCompilation.Create("test")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Task).Assembly.Location));

        var helperClass = compilation.GetTypeByMetadataName("TestNamespace.TestHelper");
        var method = (IMethodSymbol)helperClass.GetMembers("GetTask").First();
        var returnType = method.ReturnType;

        // Act
        var result = TypeValidator.IsValidVoidHandlerReturnType(returnType);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidVoidHandlerReturnType returns true when the return type is ValueTask without being an INamedTypeSymbol.
    /// This tests the fallback condition in the method.
    /// </summary>
    [Fact]
    public void IsValidVoidHandlerReturnType_ValueTaskNameOnly_ReturnsTrue()
    {
        // Setup compilation to get ValueTask type
        var sourceCode = @"
using System.Threading.Tasks;

namespace TestNamespace 
{
    public class TestHelper 
    { 
        public ValueTask GetValueTask() => default;
    } 
}";

        var compilation = CSharpCompilation.Create("test")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location));

        var helperClass = compilation.GetTypeByMetadataName("TestNamespace.TestHelper");
        var method = (IMethodSymbol)helperClass.GetMembers("GetValueTask").First();
        var returnType = method.ReturnType;

        // Act
        var result = TypeValidator.IsValidVoidHandlerReturnType(returnType);

        // Assert
        Assert.True(result);
    }
}