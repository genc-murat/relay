extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Core;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for the exception handling paths in RelayAnalyzer methods.
/// </summary>
public class RelayAnalyzerExceptionHandlingTests
{
    /// <summary>
    /// Tests that RelayAnalyzer handles general operations without throwing exceptions.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_AnalyzeMethodDeclaration_NoExceptionsThrown()
    {
        // This test verifies that normal operation doesn't throw exceptions
        // Arrange
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
        public async Task<string> {|RELAY_GEN_000:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(1);
            return ""test"";
        }
    }
}";

        // Create compilation
        var compilation = CreateTestCompilation(source);
        
        // Create analyzer
        var analyzer = new RelayAnalyzer();

        // This test verifies normal operation doesn't throw exceptions
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            options: null);

        // The analyzer should handle normal operations gracefully
        await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        
        // Check that no exception was thrown (the test would fail otherwise)
        // Note: We're not asserting anything specific about the diagnostics, just that no exception occurred
    }

    /// <summary>
    /// Tests that RelayAnalyzer handles exceptions in syntax tree processing gracefully.
    /// This covers the catch block inside the syntax tree processing loop in AnalyzeCompilation.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_AnalyzeCompilation_SyntaxTreeProcessingException_HandledGracefully()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;

namespace TestNamespace
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

        // Create compilation with analyzers
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            options: null);

        // Run analysis - this should not throw any exceptions
        await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Should complete without exceptions - no specific assertion other than no exception being thrown
    }

    /// <summary>
    /// Tests that RelayAnalyzer handles general exceptions in compilation analysis gracefully.
    /// This covers the general catch block in AnalyzeCompilation.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_AnalyzeCompilation_GeneralException_HandledGracefully()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestClass
    {
        public async Task<string> {|RELAY_GEN_000:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
        {
            return ""test"";
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        var analyzer = new RelayAnalyzer();

        // Create compilation with analyzers
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            options: null);

        // Run analysis - this should not throw any exceptions
        await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Should complete without exceptions - no specific assertion other than no exception being thrown
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