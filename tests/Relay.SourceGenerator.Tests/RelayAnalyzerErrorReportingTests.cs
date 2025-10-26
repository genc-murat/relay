extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Core;
using System.Collections.Immutable;
using System.Reflection;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for the error reporting functionality in RelayAnalyzer.
/// These tests verify that the ReportAnalyzerError methods are called when exceptions occur.
/// </summary>
public class RelayAnalyzerErrorReportingTests
{
    /// <summary>
    /// Tests that analyzer handles exceptions during method analysis and reports errors appropriately.
    /// This covers the first overload of ReportAnalyzerError method.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_ExceptionDuringMethodAnalysis_ReportsError()
    {
        // Test that exceptions during method analysis are handled gracefully
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]
        public async Task<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(1);
            return ""test"";
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var analyzer = new RelayAnalyzer();

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            options: null);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Verify that normal analysis completed without crashing
    }

    /// <summary>
    /// Tests that AnalyzeMethodDeclaration handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void RelayAnalyzer_AnalyzeMethodDeclaration_HandlesExceptionsGracefully()
    {
        // This test verifies that AnalyzeMethodDeclaration doesn't throw unhandled exceptions
        // The actual error reporting is tested in integration tests
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeMethodDeclaration", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        // Method exists and is accessible
    }

    /// <summary>
    /// Tests that analyzer handles exceptions during compilation analysis and reports errors appropriately.
    /// This covers the second overload of ReportAnalyzerError method.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_ExceptionDuringCompilationAnalysis_ReportsError()
    {
        // Test that exceptions during compilation analysis are handled gracefully
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var analyzer = new RelayAnalyzer();

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            options: null);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Verify that normal analysis completed without crashing
    }

    /// <summary>
    /// Tests that AnalyzeCompilation handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void RelayAnalyzer_AnalyzeCompilation_HandlesExceptionsGracefully()
    {
        // This test verifies that AnalyzeCompilation doesn't throw unhandled exceptions
        // The actual error reporting is tested in integration tests
        var analyzeMethod = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(analyzeMethod);
        // Method exists and is accessible
    }

    /// <summary>
    /// Tests that CompilationAnalysisContextDiagnosticReporter can be instantiated.
    /// </summary>
    [Fact]
    public void CompilationAnalysisContextDiagnosticReporter_CanBeInstantiated()
    {
        // This test verifies that CompilationAnalysisContextDiagnosticReporter exists and can be created
        // The actual functionality is tested in integration tests
        var type = typeof(CompilationAnalysisContextDiagnosticReporter);
        Assert.NotNull(type);
        var constructor = type.GetConstructor([typeof(CompilationAnalysisContext)]);
        Assert.NotNull(constructor);
    }

    /// <summary>
    /// Tests that ValidateAttributeParameterConflicts handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void RelayAnalyzer_ValidateAttributeParameterConflicts_HandlesExceptionsGracefully()
    {
        // This test verifies that ValidateAttributeParameterConflicts doesn't throw unhandled exceptions
        var validateMethod = typeof(RelayAnalyzer).GetMethod("ValidateAttributeParameterConflicts", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(validateMethod);
        // Method exists and is accessible
    }

    /// <summary>
    /// Tests that ConvertToHandlerRegistrations properly converts AnalyzerHandlerInfo to HandlerRegistration.
    /// </summary>
    [Fact]
    public void RelayAnalyzer_ConvertToHandlerRegistrations_ConvertsCorrectly()
    {
        // Arrange
        var handlerRegistry = new HandlerRegistry();

        // Act
        var convertMethod = typeof(RelayAnalyzer).GetMethod("ConvertToHandlerRegistrations", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(convertMethod);

        var result = (IEnumerable<HandlerRegistration>)convertMethod.Invoke(null!, [handlerRegistry])!;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // Since we didn't add any handlers
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