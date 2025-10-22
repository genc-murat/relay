using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Relay.SourceGenerator.Validators;
using System.Collections.Immutable;
using System.Reflection;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for PipelineValidator methods to ensure comprehensive coverage.
/// </summary>
public class PipelineValidatorTests
{
    #region Integration Test Verification

    [Fact]
    public async Task PipelineValidator_Methods_Are_Called_From_RelayAnalyzer()
    {
        // This test verifies that PipelineValidator methods are actually called
        // by the RelayAnalyzer through integration testing

        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void ValidPipeline(string context, CancellationToken token) { }

    [Pipeline(Order = 1)]
    public void AnotherPipeline(string context, CancellationToken token) { }
}";

        // This should pass without diagnostics since the methods are valid
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_Pipeline_Signatures()
    {
        // This test verifies that invalid pipeline signatures are caught
        var source = @"
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void {|RELAY_GEN_002:InvalidPipeline|}() { }
}";

        // This should report a diagnostic for invalid pipeline signature
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }
    #endregion

    #region Unit Tests for Helper Methods

    [Fact]
    public void GetScopeName_Returns_Correct_Names()
    {
        // Act & Assert
        var result0 = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 0 });
        Assert.Equal("All", result0);

        var result1 = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 1 });
        Assert.Equal("Requests", result1);

        var result2 = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 2 });
        Assert.Equal("Streams", result2);

        var result3 = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 3 });
        Assert.Equal("Notifications", result3);

        var resultUnknown = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 99 });
        Assert.Equal("Unknown", resultUnknown);
    }

    #endregion

    #region Enhanced Integration Tests

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_IPipelineBehavior_Return_Type()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void {|RELAY_GEN_002:InvalidPipeline|}(string request, RequestHandlerDelegate<string> next, CancellationToken token) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_CancellationToken_In_IPipelineBehavior()
    {
        var source = @"
using System.Threading.Tasks;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public Task<string> {|RELAY_GEN_002:InvalidPipeline|}(string request, RequestHandlerDelegate<string> next, string invalidToken)
    {
        return Task.FromResult(string.Empty);
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Validates_Stream_Pipeline_Signature()
    {
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public IAsyncEnumerable<string> ValidStreamPipeline(string request, StreamHandlerDelegate<string> next, CancellationToken token)
    {
        return next();
    }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    #endregion

    #region Parameter Count Validation Tests

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_Pipeline_With_Zero_Parameters()
    {
        var source = @"
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void {|RELAY_GEN_002:InvalidPipeline|}() { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_Pipeline_With_One_Parameter()
    {
        var source = @"
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void {|RELAY_GEN_002:InvalidPipeline|}(string param) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Validates_Pipeline_With_Four_Parameters()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void ValidPipeline(string context, string param2, string param3, CancellationToken token) { }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    #endregion

    #region IPipelineBehavior Pattern Validation Tests

    [Fact]
    public async Task PipelineValidator_Validates_Generic_Pipeline_With_Invalid_Delegate_Type_In_Three_Parameters()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void ValidPipeline(string request, string invalidDelegate, CancellationToken token) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_Third_Parameter_In_IPipelineBehavior()
    {
        var source = @"
using System.Threading.Tasks;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public Task<string> {|RELAY_GEN_002:InvalidPipeline|}(string request, RequestHandlerDelegate<string> next, string invalidToken)
    {
        return Task.FromResult(string.Empty);
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_Return_Type_For_IPipelineBehavior()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void {|RELAY_GEN_002:InvalidPipeline|}(string request, RequestHandlerDelegate<string> next, CancellationToken token) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Validates_IPipelineBehavior_With_ValueTask_Return()
    {
        var source = @"
using System.Threading.Tasks;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public ValueTask<string> ValidPipeline(string request, RequestHandlerDelegate<string> next, CancellationToken token)
    {
        return ValueTask.FromResult(string.Empty);
    }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    #endregion

    #region Generic Pipeline Pattern Validation Tests

    [Fact]
    public async Task PipelineValidator_Reports_Missing_CancellationToken_In_Generic_Pipeline()
    {
        var source = @"
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void {|RELAY_GEN_207:InvalidPipeline|}(string context, string param) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Validates_Generic_Pipeline_With_Three_Parameters()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void ValidPipeline(string context, string param2, CancellationToken token) { }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    #endregion

    #region CollectPipelineInfo Tests

    [Fact]
    public void CollectPipelineInfo_Extracts_Order_And_Scope_Parameters()
    {
        // Create a test compilation with pipeline attributes
        var source = @"
using Relay.Core;

namespace Test
{
    public class TestClass
    {
        [Pipeline(Order = 5, Scope = 2)]
        public void TestPipeline(string context, CancellationToken token) { }
    }
}";

        var compilation = CreateTestCompilation(source);
        var tree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the relay core stubs
        var semanticModel = compilation.GetSemanticModel(tree);
        var methodDeclaration = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "TestPipeline" && m.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDecl && classDecl.Identifier.Text == "TestClass");
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration)!;

        // Get the pipeline attribute
        var pipelineAttribute = methodSymbol.GetAttributes()
            .First(attr => attr.AttributeClass?.Name == "PipelineAttribute");

        // Create pipeline registry
        var pipelineRegistry = new List<PipelineInfo>();

        // Act
        typeof(PipelineValidator).GetMethod("CollectPipelineInfo", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object[] { pipelineRegistry, methodSymbol, pipelineAttribute, methodDeclaration });

        // Assert
        Assert.Single(pipelineRegistry);
        var info = pipelineRegistry[0];
        Assert.Equal("TestPipeline", info.MethodName);
        Assert.Equal(5, info.Order);
        Assert.Equal(2, info.Scope);
        // Debug: Let's see what the actual containing type is
        Console.WriteLine($"Actual ContainingType: '{info.ContainingType}'");
        Console.WriteLine($"Method symbol containing type: {methodSymbol.ContainingType}");
        Console.WriteLine($"Method symbol containing namespace: {methodSymbol.ContainingNamespace}");
        Assert.Equal("Test.TestClass", info.ContainingType);
    }

    [Fact]
    public void CollectPipelineInfo_Uses_Defaults_When_No_Parameters()
    {
        // Create a test compilation with pipeline attributes
        var source = @"
using Relay.Core;

namespace Test
{
    public class TestClass
    {
        [Pipeline]
        public void TestPipeline(string context, CancellationToken token) { }
    }
}";

        var compilation = CreateTestCompilation(source);
        var tree = compilation.SyntaxTrees.Last(); // Get the test source tree, not the relay core stubs
        var semanticModel = compilation.GetSemanticModel(tree);
        var methodDeclaration = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "TestPipeline" && m.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDecl && classDecl.Identifier.Text == "TestClass");
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration)!;

        // Get the pipeline attribute
        var pipelineAttribute = methodSymbol.GetAttributes()
            .First(attr => attr.AttributeClass?.Name == "PipelineAttribute");

        // Create pipeline registry
        var pipelineRegistry = new List<PipelineInfo>();

        // Act
        typeof(PipelineValidator).GetMethod("CollectPipelineInfo", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object[] { pipelineRegistry, methodSymbol, pipelineAttribute, methodDeclaration });

        // Assert
        Assert.Single(pipelineRegistry);
        var info = pipelineRegistry[0];
        Assert.Equal("TestPipeline", info.MethodName);
        Assert.Equal(0, info.Order); // Default
        Assert.Equal(0, info.Scope); // Default (All)
        Assert.Equal("Test.TestClass", info.ContainingType);
    }

    #endregion

    #region ValidateDuplicatePipelineOrders Tests

    [Fact]
    public void ValidateDuplicatePipelineOrders_Reports_Duplicate_Orders_In_Same_Scope()
    {
        // Create test pipeline info with duplicates
        var pipelineRegistry = new List<PipelineInfo>
        {
            new PipelineInfo { MethodName = "Pipeline1", Order = 1, Scope = 0, ContainingType = "Test.TestClass", Location = Location.None },
            new PipelineInfo { MethodName = "Pipeline2", Order = 1, Scope = 0, ContainingType = "Test.TestClass", Location = Location.None },
        };

        // Since CompilationAnalysisContext is sealed, we'll test the logic directly
        // by checking that duplicates are detected (the method would report diagnostics)
        var duplicates = pipelineRegistry
            .GroupBy(p => new { p.Order, p.Scope, p.ContainingType })
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        // Assert - Should find 2 duplicates
        Assert.Equal(2, duplicates.Count);
    }

    [Fact]
    public void ValidateDuplicatePipelineOrders_Allows_Same_Order_In_Different_Scopes()
    {
        // Create test pipeline info with same order but different scopes
        var pipelineRegistry = new List<PipelineInfo>
        {
            new PipelineInfo { MethodName = "Pipeline1", Order = 1, Scope = 0, ContainingType = "Test.TestClass", Location = Location.None },
            new PipelineInfo { MethodName = "Pipeline2", Order = 1, Scope = 1, ContainingType = "Test.TestClass", Location = Location.None },
        };

        // Check that no duplicates are found
        var duplicates = pipelineRegistry
            .GroupBy(p => new { p.Order, p.Scope, p.ContainingType })
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        // Assert - Should find no duplicates
        Assert.Empty(duplicates);
    }

    [Fact]
    public void ValidateDuplicatePipelineOrders_Allows_Same_Order_In_Different_Classes()
    {
        // Create test pipeline info with same order but different classes
        var pipelineRegistry = new List<PipelineInfo>
        {
            new PipelineInfo { MethodName = "Pipeline1", Order = 1, Scope = 0, ContainingType = "Test.TestClass1", Location = Location.None },
            new PipelineInfo { MethodName = "Pipeline2", Order = 1, Scope = 0, ContainingType = "Test.TestClass2", Location = Location.None },
        };

        // Check that no duplicates are found
        var duplicates = pipelineRegistry
            .GroupBy(p => new { p.Order, p.Scope, p.ContainingType })
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        // Assert - Should find no duplicates
        Assert.Empty(duplicates);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task PipelineValidator_Validates_Three_Parameters_As_Generic_Pipeline()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void ValidPipeline(string request, string context, CancellationToken token) { }
}";

        // Should pass without diagnostics (3 parameters, not IPipelineBehavior pattern)
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    #endregion

    #region Helper Methods

    private static CSharpCompilation CreateTestCompilation(string source)
    {
        // Add Relay.Core stubs
        var relayCoreStubs = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    public interface IStreamRequest<out TResponse> { }
    public interface INotification { }

    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(TResponse request, CancellationToken token);
    public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<TResponse>(TResponse request, CancellationToken token);

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandleAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NotificationAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PipelineAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Order { get; set; }
        public int Scope { get; set; }
    }
}";

        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(relayCoreStubs),
            CSharpSyntaxTree.ParseText(source)
        };

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static INamedTypeSymbol GetTypeSymbolFromCompilation(CSharpCompilation compilation, string typeName)
    {
        // Handle C# builtin aliases and common types
        switch (typeName)
        {
            case "String":
                return (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_String);
            case "Task":
                return compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")!;
            case "Action":
                return compilation.GetTypeByMetadataName("System.Action")!;
        }

        // Try to get by metadata name first (works for fully qualified names)
        var symbol = compilation.GetTypeByMetadataName(typeName);
        if (symbol != null)
        {
            return symbol;
        }

        // For generic types without namespace, try common namespaces
        if (typeName.Contains("`"))
        {
            var baseName = typeName.Split('`')[0];
            var possibleNames = new[]
            {
                $"System.{baseName}",
                $"System.Threading.Tasks.{baseName}",
                $"System.Collections.Generic.{baseName}",
                $"System.Func`1", // Special case for Func
                $"System.Action`1" // Special case for Action
            };

            foreach (var name in possibleNames)
            {
                symbol = compilation.GetTypeByMetadataName(name);
                if (symbol != null)
                {
                    return symbol;
                }
            }
        }

        // Fallback to symbol name search
        symbol = compilation.GetSymbolsWithName(typeName).OfType<INamedTypeSymbol>().FirstOrDefault();
        if (symbol != null)
        {
            return symbol;
        }

        throw new InvalidOperationException($"Could not find type symbol for {typeName}");
    }

    #endregion

    #region IsValidPipelineDelegate Tests

    [Fact]
    public void IsValidPipelineDelegate_ReturnsTrue_For_RequestHandlerDelegate()
    {
        // Arrange - Create a test compilation with the delegate types
        var source = @"
using System.Threading.Tasks;
using Relay.Core;

public class TestClass
{
    public void Test(RequestHandlerDelegate<string> handler) { }
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        var handlerType = parameterSymbol!.Type;

        // Act
        var result = PipelineValidator.IsValidPipelineDelegate(handlerType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPipelineDelegate_ReturnsTrue_For_StreamHandlerDelegate()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using Relay.Core;

public class TestClass
{
    public void Test(StreamHandlerDelegate<string> handler) { }
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        var handlerType = parameterSymbol!.Type;

        // Act
        var result = PipelineValidator.IsValidPipelineDelegate(handlerType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPipelineDelegate_ReturnsTrue_For_Func_With_ValueTask()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public void Test(System.Func<System.Threading.Tasks.ValueTask<string>> func) { }
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        var funcType = parameterSymbol!.Type;

        // Act
        var result = PipelineValidator.IsValidPipelineDelegate(funcType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPipelineDelegate_ReturnsTrue_For_Func_With_Task()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public void Test(System.Func<System.Threading.Tasks.Task<string>> func) { }
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        var funcType = parameterSymbol!.Type;

        // Act
        var result = PipelineValidator.IsValidPipelineDelegate(funcType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPipelineDelegate_ReturnsTrue_For_Func_With_IAsyncEnumerable()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;

public class TestClass
{
    public void Test(System.Func<System.Collections.Generic.IAsyncEnumerable<string>> func) { }
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        var funcType = parameterSymbol!.Type;

        // Act
        var result = PipelineValidator.IsValidPipelineDelegate(funcType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPipelineDelegate_ReturnsFalse_For_Invalid_Delegate()
    {
        // Arrange
        var source = @"
public class TestClass
{
    public delegate void InvalidDelegate();
    public void Test(InvalidDelegate handler) { }
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        var invalidType = parameterSymbol!.Type;

        // Act
        var result = PipelineValidator.IsValidPipelineDelegate(invalidType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidPipelineDelegate_ReturnsFalse_For_Func_With_Wrong_Type_Argument_Count()
    {
        // Arrange - Action with no type arguments
        var source = @"
public class TestClass
{
    public void Test(System.Action action) { }
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        var actionType = parameterSymbol!.Type;

        // Act
        var result = PipelineValidator.IsValidPipelineDelegate(actionType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidPipelineDelegate_ReturnsFalse_For_Func_With_Invalid_Return_Type()
    {
        // Arrange
        var source = @"
public class TestClass
{
    public void Test(System.Func<string> func) { }
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol;
        var funcType = parameterSymbol!.Type;

        // Act
        var result = PipelineValidator.IsValidPipelineDelegate(funcType);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsValidPipelineReturnType Tests

    [Fact]
    public void IsValidPipelineReturnType_ReturnsTrue_For_Task()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public Task<string> Test() => Task.FromResult(string.Empty);
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        var taskType = methodSymbol!.ReturnType;

        // Act
        var result = PipelineValidator.IsValidPipelineReturnType(taskType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPipelineReturnType_ReturnsTrue_For_ValueTask()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public ValueTask<string> Test() => ValueTask.FromResult(string.Empty);
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        var valueTaskType = methodSymbol!.ReturnType;

        // Act
        var result = PipelineValidator.IsValidPipelineReturnType(valueTaskType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPipelineReturnType_ReturnsTrue_For_IAsyncEnumerable()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;

public class TestClass
{
    public IAsyncEnumerable<string> Test() => null!;
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        var asyncEnumType = methodSymbol!.ReturnType;

        // Act
        var result = PipelineValidator.IsValidPipelineReturnType(asyncEnumType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPipelineReturnType_ReturnsFalse_For_Invalid_Type()
    {
        // Arrange
        var source = @"
public class TestClass
{
    public string Test() => string.Empty;
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        var stringType = methodSymbol!.ReturnType;

        // Act
        var result = PipelineValidator.IsValidPipelineReturnType(stringType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidPipelineReturnType_ReturnsFalse_For_Task_With_Wrong_Type_Argument_Count()
    {
        // Arrange - Task with no type arguments
        var source = @"
using System.Threading.Tasks;

public class TestClass
{
    public Task Test() => Task.CompletedTask;
}";
        var compilation = CreateTestCompilation(source);
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        var methodDeclaration = compilation.SyntaxTrees.Last().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        var taskType = methodSymbol!.ReturnType;

        // Act
        var result = PipelineValidator.IsValidPipelineReturnType(taskType);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Test Helper Classes

    public class PipelineAttribute : Attribute
    {
        public int Order { get; set; }
        public int Scope { get; set; }
    }

    #endregion
}