extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Relay.SourceGenerator;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for the RelayAnalyzer to ensure proper validation of handler signatures and configurations.
    /// </summary>
    public class RelayAnalyzerTests
    {
        /// <summary>
        /// Tests that valid handler methods do not produce diagnostics.
        /// </summary>
        [Fact]
        public async Task ValidHandler_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers missing request parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerMissingRequestParameter_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_205:HandleAsync|}()
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with invalid return types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerInvalidReturnType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public {|RELAY_GEN_202:int|} {|RELAY_GEN_102:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return 42;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that duplicate handlers produce diagnostics.
        /// </summary>
        [Fact]
        public async Task DuplicateHandlers_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler1
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test1"");
    }
}

public class TestHandler2
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test2"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with invalid signatures produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerInvalidSignature_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public {|RELAY_GEN_204:string|} {|RELAY_GEN_102:HandleAsync|}(TestNotification notification, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers missing CancellationToken produce warnings.
        /// </summary>
        [Fact]
        public async Task HandlerMissingCancellationToken_ProducesWarning()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Helper method to verify analyzer diagnostics.
        /// </summary>
        private static async Task VerifyAnalyzerAsync(string source)
        {
            // Create compilation
            var compilation = CreateTestCompilation(source);
            
            // Create analyzer
            var analyzer = new RelayAnalyzer();
            
            // Create compilation with analyzers
            var compilationWithAnalyzers = compilation.WithAnalyzers(
                ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            
            // Get analyzer diagnostics
            var analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            
            // Get expected diagnostics from markup in source
            var expectedDiagnostics = ParseExpectedDiagnostics(source);
            
            // Verify diagnostics match expectations
            VerifyDiagnostics(expectedDiagnostics, analyzerDiagnostics);
        }

        /// <summary>
        /// Creates a test compilation with the provided source.
        /// </summary>
        private static CSharpCompilation CreateTestCompilation(string source)
        {
            // Remove diagnostic markup from source before compilation
            var cleanSource = RemoveDiagnosticMarkup(source);

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
    }
}";

            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(relayCoreStubs),
                CSharpSyntaxTree.ParseText(cleanSource)
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

        /// <summary>
        /// Removes diagnostic markup from source code.
        /// </summary>
        private static string RemoveDiagnosticMarkup(string source)
        {
            var result = source;
            var startIndex = 0;

            while ((startIndex = result.IndexOf("{|", startIndex)) != -1)
            {
                var endIndex = result.IndexOf("|}", startIndex);
                if (endIndex == -1) break;

                var beforeMarkup = result.Substring(0, startIndex);
                var afterMarkup = result.Substring(endIndex + 2);
                
                // Find the content between the markup (the actual code)
                var markupContent = result.Substring(startIndex + 2, endIndex - startIndex - 2);
                var colonIndex = markupContent.IndexOf(':');
                var content = colonIndex >= 0 ? markupContent.Substring(colonIndex + 1) : "";

                result = beforeMarkup + content + afterMarkup;
                startIndex = beforeMarkup.Length + content.Length;
            }

            return result;
        }

        /// <summary>
        /// Parses expected diagnostic markers from the source code.
        /// </summary>
        private static List<ExpectedDiagnostic> ParseExpectedDiagnostics(string source)
        {
            var expectedDiagnostics = new List<ExpectedDiagnostic>();
            var lines = source.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var startIndex = 0;

                while ((startIndex = line.IndexOf("{|", startIndex)) != -1)
                {
                    var endIndex = line.IndexOf("|}", startIndex);
                    if (endIndex == -1) break;

                    var diagnosticId = line.Substring(startIndex + 2, endIndex - startIndex - 2);
                    var colonIndex = diagnosticId.IndexOf(':');
                    
                    if (colonIndex != -1)
                    {
                        diagnosticId = diagnosticId.Substring(0, colonIndex);
                    }

                    expectedDiagnostics.Add(new ExpectedDiagnostic
                    {
                        Id = diagnosticId,
                        Line = i + 1,
                        Column = startIndex + 1
                    });

                    startIndex = endIndex + 2;
                }
            }

            return expectedDiagnostics;
        }

        /// <summary>
        /// Verifies that actual diagnostics match expected diagnostics.
        /// </summary>
        private static void VerifyDiagnostics(List<ExpectedDiagnostic> expectedDiagnostics, ImmutableArray<Diagnostic> actualDiagnostics)
        {
            var actualErrors = actualDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning).ToList();

            if (expectedDiagnostics.Count == 0 && actualErrors.Count == 0)
            {
                return; // Both empty, test passes
            }

            // Check each expected diagnostic is present
            foreach (var expected in expectedDiagnostics)
            {
                var matchingDiagnostic = actualErrors.FirstOrDefault(d => d.Id == expected.Id);
                Assert.NotNull(matchingDiagnostic);
            }

            // Check we don't have unexpected diagnostics
            foreach (var actual in actualErrors)
            {
                var expectedForThisDiagnostic = expectedDiagnostics.Any(e => e.Id == actual.Id);
                Assert.True(expectedForThisDiagnostic, $"Unexpected diagnostic '{actual.Id}': {actual.GetMessage()}");
            }
        }

        /// <summary>
        /// Represents an expected diagnostic from test markup.
        /// </summary>
        private class ExpectedDiagnostic
        {
            public string Id { get; set; } = string.Empty;
            public int Line { get; set; }
            public int Column { get; set; }
        }
    }
}