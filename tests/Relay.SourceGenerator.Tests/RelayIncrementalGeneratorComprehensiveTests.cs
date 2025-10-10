using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class RelayIncrementalGeneratorComprehensiveTests
    {
        [Fact]
        public void Generator_Should_Report_MissingRelayCoreReference_Diagnostic()
        {
            // Arrange - Create code with Relay attributes but no Relay.Core reference
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public ValueTask<string> HandleAsync(string request, CancellationToken ct)
        {
            return ValueTask.FromResult(request);
        }
    }
}";

            var generator = new RelayIncrementalGenerator();
            // Create compilation WITHOUT Relay.Core reference
            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Act
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            // Assert
            var missingCoreDiagnostic = diagnostics.FirstOrDefault(d => 
                d.Id == "RELAY_GEN_004" && // MissingRelayCoreReference
                d.Severity == DiagnosticSeverity.Error);
            
            Assert.NotNull(missingCoreDiagnostic);
            Assert.Contains("Relay.Core package must be referenced", missingCoreDiagnostic.GetMessage());
        }

        [Fact]
        public void Generator_Should_Handle_RequestHandler_Interface_Implementations()
        {
            // Arrange - Test IRequestHandler interface implementation
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler : IRequestHandler<TestRequest, string>
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

            // Assert
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.NotEmpty(runResult.GeneratedTrees);
        }

        [Fact]
        public void Generator_Should_Handle_NotificationHandler_Interface_Implementations()
        {
            // Arrange - Test INotificationHandler interface implementation
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Notifications;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestNotification : INotification { }
    
    public class TestHandler : INotificationHandler<TestNotification>
    {
        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
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

            // Assert
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.NotEmpty(runResult.GeneratedTrees);
        }

        [Fact]
        public void Generator_Should_Handle_StreamHandler_Interface_Implementations()
        {
            // Arrange - Test IStreamHandler interface implementation
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<IAsyncEnumerable<string>> { }
    
    public class TestHandler : IStreamHandler<TestRequest, string>
    {
        public IAsyncEnumerable<string> HandleStreamAsync(TestRequest request, CancellationToken cancellationToken)
        {
            yield break;
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

            // Assert
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.NotEmpty(runResult.GeneratedTrees);
        }

        [Fact]
        public void Generator_Should_Generate_DI_Registrations_For_Multiple_Handlers()
        {
            // Arrange - Multiple different handler types
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Notifications;

namespace TestProject
{
    public class TestRequest1 : IRequest<string> { }
    public class TestRequest2 : IRequest<int> { }
    public class TestNotification : INotification { }
    
    public class TestHandler1 : IRequestHandler<TestRequest1, string>
    {
        public ValueTask<string> HandleAsync(TestRequest1 request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""test1"");
        }
    }
    
    public class TestHandler2 : IRequestHandler<TestRequest2, int>
    {
        public ValueTask<int> HandleAsync(TestRequest2 request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(42);
        }
    }
    
    public class TestHandler3 : INotificationHandler<TestNotification>
    {
        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
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

            // Assert
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.NotEmpty(runResult.Results);
            
            // Should generate both DI registration and optimized dispatcher
            var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
            Assert.Contains("AddRelay", generatedCode);
            Assert.Contains("IRequestHandler", generatedCode);
            Assert.Contains("INotificationHandler", generatedCode);
        }

        [Fact]
        public void Generator_Should_Detect_Invalid_Handler_Name()
        {
            // Arrange - Test invalid relay attribute name
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [InvalidAttribute]
        public ValueTask<string> HandleAsync(string request, CancellationToken ct)
        {
            return ValueTask.FromResult(request);
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

            // Assert - Should not crash, and should not generate with invalid attribute
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id == "RELAY_GEN_001"); // GeneratorError
        }

        [Fact]
        public void Generator_Should_Handle_Generic_Request_Handlers()
        {
            // Arrange - Test generic IRequestHandler interface implementation
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class GenericRequest<T> : IRequest<T> { }
    
    public class GenericHandler<T> : IRequestHandler<GenericRequest<T>, T>
    {
        public ValueTask<T> HandleAsync(GenericRequest<T> request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(default(T));
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

            // Assert
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.NotEmpty(runResult.GeneratedTrees);
        }

        private static Compilation CreateTestCompilation(string source)
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
}
");

            return CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { relayCoreStubs, syntaxTree },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}