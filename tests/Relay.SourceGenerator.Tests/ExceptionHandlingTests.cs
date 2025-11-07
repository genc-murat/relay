using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Core;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class ExceptionHandlingTests
    {
        [Fact]
        public void Generator_Should_Handle_Exception_In_DI_Generation()
        {
            // Arrange - Set up test to force an exception in DI registration generation
            RelayIncrementalGenerator.TestForceException = true;
            
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class ValidHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test"");
        }
    }
}";

            var generator = new RelayIncrementalGenerator();
            var compilation = CreateTestCompilation(source);

            // Act
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            // Assert - Should catch the exception and report it as a diagnostic
            var errorDiagnostic = diagnostics.FirstOrDefault(d => d.Id == "RELAY_GEN_001");
            Assert.NotNull(errorDiagnostic);
            Assert.Contains("Test exception", errorDiagnostic.GetMessage());
        }

        [Fact]
        public void Generator_Should_Not_Throw_Exception_When_Force_Exception_Flag_Is_False()
        {
            // Arrange - Ensure the test flag is false (normal execution)
            RelayIncrementalGenerator.TestForceException = false; // This is the default, but being explicit
            
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class ValidHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test"");
        }
    }
}";

            var generator = new RelayIncrementalGenerator();
            var compilation = CreateTestCompilation(source);

            // Act
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            var runResult = driver.GetRunResult();

            // Assert - Should not have exception-related diagnostics
            Assert.DoesNotContain(diagnostics, d => d.Id == "RELAY_GEN_001"); // GeneratorError
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(runResult.GeneratedTrees);
        }
        
        [Fact]
        public void IsHandlerInterface_Method_Should_Return_Correct_Values()
        {
            // Test the static method directly
            Assert.True(RelayIncrementalGenerator.IsHandlerInterface("IRequestHandler<string, int>"));
            Assert.True(RelayIncrementalGenerator.IsHandlerInterface("INotificationHandler<string>"));
            Assert.True(RelayIncrementalGenerator.IsHandlerInterface("IStreamHandler<string, int>"));
            Assert.False(RelayIncrementalGenerator.IsHandlerInterface("IFooBar<string, int>"));
            Assert.False(RelayIncrementalGenerator.IsHandlerInterface("SomeOtherInterface"));
            Assert.False(RelayIncrementalGenerator.IsHandlerInterface(""));
        }
        
        [Fact]
        public void GenerateOptimizedDispatchers_Should_Handle_Exception_And_Use_Fallback()
        {
            // Arrange - Create a scenario that will cause an exception in optimized dispatcher generation
            // We'll use the TestForceException flag to simulate an exception
            RelayIncrementalGenerator.TestForceException = true;
            
            try
            {
                var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class ValidHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test"");
        }
    }
}";

                var generator = new RelayIncrementalGenerator();
                var compilation = CreateTestCompilation(source);

                // Act
                var driver = CSharpGeneratorDriver.Create(generator);
                driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                    compilation,
                    out var outputCompilation,
                    out var diagnostics);

                // Assert - Should catch the exception and generate fallback dispatcher
                var runResult = driver.GetRunResult();
                
                // Should have generated some output (fallback)
                Assert.NotNull(runResult.GeneratedTrees);
                Assert.True(runResult.GeneratedTrees.Length > 0);
                
                // Should contain the fallback dispatcher source
                var generatedSources = runResult.GeneratedTrees
                    .Where(t => t.FilePath.Contains("OptimizedRequestDispatcher.g.cs"))
                    .ToList();
                
                Assert.True(generatedSources.Count > 0, "Should generate fallback optimized dispatcher");
                
                // The generated source should contain fallback indicators
                var fallbackSource = generatedSources.First().ToString();
                Assert.Contains("fallback", fallbackSource, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                // Cleanup - Reset the test flag
                RelayIncrementalGenerator.TestForceException = false;
            }
        }

        [Fact]
        public void IsRelayAttributeName_Method_Should_Return_Correct_Values()
        {
            // Test the static method directly
            Assert.True(RelayIncrementalGenerator.IsRelayAttributeName("Handle"));
            Assert.True(RelayIncrementalGenerator.IsRelayAttributeName("Notification"));
            Assert.True(RelayIncrementalGenerator.IsRelayAttributeName("Pipeline"));
            Assert.True(RelayIncrementalGenerator.IsRelayAttributeName("ExposeAsEndpoint"));
            
            // Test with Attribute suffix
            Assert.True(RelayIncrementalGenerator.IsRelayAttributeName("HandleAttribute"));
            Assert.True(RelayIncrementalGenerator.IsRelayAttributeName("NotificationAttribute"));
            Assert.True(RelayIncrementalGenerator.IsRelayAttributeName("PipelineAttribute"));
            Assert.True(RelayIncrementalGenerator.IsRelayAttributeName("ExposeAsEndpointAttribute"));
            
            // Test non-relay names
            Assert.False(RelayIncrementalGenerator.IsRelayAttributeName("SomeOtherAttribute"));
            Assert.False(RelayIncrementalGenerator.IsRelayAttributeName("Foo"));
            Assert.False(RelayIncrementalGenerator.IsRelayAttributeName(""));
            Assert.False(RelayIncrementalGenerator.IsRelayAttributeName(null));
        }

        private static CSharpCompilation CreateTestCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Contracts.Requests
{
    public interface IRequest<out TResponse> { }
}

namespace Relay.Core.Contracts.Notifications
{
    public interface INotification { }
}

namespace Relay.Core.Contracts.Handlers
{
    public interface IRequestHandler<in TRequest, TResponse>
    {
        ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
    
    public interface INotificationHandler<in TNotification>
    {
        ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken);
    }
    
    public interface IStreamHandler<in TRequest, TResponse>
    {
        IAsyncEnumerable<TResponse> HandleStreamAsync(TRequest request, CancellationToken cancellationToken);
    }
}

namespace Relay.Core
{
    public interface IRequest<out TResponse> { }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandleAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NotificationAttribute : Attribute
    {
        public int Priority { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PipelineAttribute : Attribute
    {
        public int Order { get; set; }
        public string? Scope { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ExposeAsEndpointAttribute : Attribute
    {
        public string? Route { get; set; }
    }
}
");

            return CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: [relayCoreStubs, syntaxTree],
                references:
                [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                ],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}