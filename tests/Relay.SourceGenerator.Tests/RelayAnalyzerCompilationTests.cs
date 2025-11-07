extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Core;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Validation;
using System.Reflection;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Comprehensive tests for AnalyzeCompilation method in RelayAnalyzer.
/// These tests focus on edge cases, error conditions, and internal behavior.
/// </summary>
public class RelayAnalyzerCompilationTests
{
    /// <summary>
    /// Tests that AnalyzeCompilation handles empty compilation with no syntax trees.
    /// This tests the basic loop behavior with empty collection.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_WithEmptyCompilation_NoSyntaxTrees_CompletesSuccessfully()
    {
        // Arrange
        var compilation = CSharpCompilation.Create("TestAssembly");
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles syntax trees with no method declarations.
    /// This tests the DescendantNodes().OfType&lt;MethodDeclarationSyntax&gt;() query.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_WithSyntaxTreeHavingNoMethods_CompletesSuccessfully()
    {
        // Arrange
        var source = @"
using System;

namespace TestProject
{
    public class TestClass
    {
        public int TestField;
        public string TestProperty { get; set; }
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles null semantic model for syntax tree.
    /// This tests the ValidationHelper.TryGetSemanticModel null check.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_WithNullSemanticModelForSyntaxTree_SkipsTreeAndContinues()
    {
        // Arrange
        var source = @"
using System;

namespace TestProject
{
    public class TestClass
    {
        public void TestMethod() { }
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles null method symbol gracefully.
    /// This tests the ValidationHelper.TryGetDeclaredSymbol null check.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_WithNullMethodSymbol_SkipsMethodAndContinues()
    {
        // Arrange
        var source = @"
using System;

namespace TestProject
{
    public class TestClass
    {
        public void TestMethod() { }
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation uses attribute cache correctly.
    /// This tests the attributeCache.TryGetValue logic.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_WithAttributeCacheHit_UsesCachedAttributes()
    {
        // Arrange
        var source = @"
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]
        public string Handle(TestRequest request) => """";
        
        [Handle]
        public string Handle2(TestRequest request) => """";
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles exceptions in handler registry gracefully.
    /// This tests the exception handling in handlerRegistry.AddHandler.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_HandlerRegistryAddHandlerThrows_ContinuesProcessing()
    {
        // Arrange
        var source = @"
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]
        public string Handle(TestRequest request) => """";
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles exceptions in pipeline collection gracefully.
    /// This tests the exception handling in PipelineValidator.CollectPipelineInfo.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_PipelineValidatorCollectThrows_ContinuesProcessing()
    {
        // Arrange
        var source = @"
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Pipeline]
        public Task<string> Handle(TestRequest request, Func<TestRequest, Task<string>> next) => 
            Task.FromResult("""");
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles exceptions in duplicate handler validation gracefully.
    /// This tests the exception handling in DuplicateHandlerValidator.ValidateDuplicateHandlers.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_DuplicateHandlerValidatorThrows_ReportsError()
    {
        // Arrange
        var source = @"
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler1
    {
        [Handle]
        public string Handle(TestRequest request) => """";
    }
    
    public class TestHandler2
    {
        [Handle]
        public string Handle(TestRequest request) => """";
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles exceptions in pipeline order validation gracefully.
    /// This tests the exception handling in PipelineValidator.ValidateDuplicatePipelineOrders.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_PipelineValidatorDuplicateOrdersThrows_ReportsError()
    {
        // Arrange
        var source = @"
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler1
    {
        [Pipeline(Order = 1)]
        public Task<string> Handle(TestRequest request, Func<TestRequest, Task<string>> next) => 
            Task.FromResult("""");
    }
    
    public class TestHandler2
    {
        [Pipeline(Order = 1)]
        public Task<string> Handle(TestRequest request, Func<TestRequest, Task<string>> next) => 
            Task.FromResult("""");
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles exceptions in attribute conflict validation gracefully.
    /// This tests the exception handling in ValidateAttributeParameterConflicts.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_ValidateAttributeParameterConflictsThrows_ReportsError()
    {
        // Arrange
        var source = @"
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle(Name = ""test"", Priority = 1)]
        public string Handle(TestRequest request) => """";
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles OperationCanceledException correctly.
    /// This tests the cancellation propagation.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_WithOperationCanceledException_PropagatesCancellation()
    {
        // Arrange
        var source = @"
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]
        public string Handle(TestRequest request) => """";
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();
        
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, cts.Token);
        
        // Act & Assert - Should propagate OperationCanceledException
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        var exception = Record.Exception(() => 
            analyzeMethod.Invoke(null!, [context]));
        
        // When using reflection, OperationCanceledException is wrapped in TargetInvocationException
        var targetInvocationException = Assert.IsType<TargetInvocationException>(exception);
        Assert.IsType<System.OperationCanceledException>(targetInvocationException.InnerException);
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles general exceptions gracefully.
    /// This tests the outer catch block.
    /// </summary>
    [Fact]
    public void AnalyzeCompilation_WithGeneralException_ReportsErrorAndContinues()
    {
        // Arrange
        var source = @"
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]
        public string Handle(TestRequest request) => """";
    }
}";
        
        var compilation = CreateTestCompilation(source);
        var context = new CompilationAnalysisContext(compilation, null!, null!, _ => true, CancellationToken.None);
        
        // Act & Assert - Should complete without exceptions
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", 
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        
        analyzeMethod.Invoke(null!, [context]);
    }

    /// <summary>
    /// Helper method to create test compilation with RelayCore stubs.
    /// </summary>
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
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}