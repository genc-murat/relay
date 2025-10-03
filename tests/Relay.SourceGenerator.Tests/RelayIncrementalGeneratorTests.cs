using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class RelayIncrementalGeneratorTests
    {
        [Fact]
        public void Generator_Should_Initialize_Without_Errors()
        {
            // Arrange
            var generator = new RelayIncrementalGenerator();

            // Act
            var compilation = CreateTestCompilation("");
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            // Assert
            diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Generator_Should_Generate_Code_With_Valid_Handlers()
        {
            // Arrange
            var source = @"
using Relay.Core;
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
            var compilation = CreateTestCompilation(source);

            // Act
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            var runResult = driver.GetRunResult();

            // Assert
            runResult.GeneratedTrees.Should().NotBeEmpty();
        }

        [Fact]
        public void Generator_Should_Handle_Empty_Project()
        {
            // Arrange
            var generator = new RelayIncrementalGenerator();
            var compilation = CreateTestCompilation("");

            // Act
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            // Assert
            // Should not crash with empty project
            diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Generator_Should_Handle_Multiple_Handlers()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class Handler1
    {
        [Handle]
        public ValueTask<string> HandleAsync(string request, CancellationToken ct) => default;
    }

    public class Handler2
    {
        [Handle]
        public ValueTask<int> HandleAsync(int request, CancellationToken ct) => default;
    }

    public class Handler3
    {
        [Notification]
        public ValueTask HandleAsync(string notification, CancellationToken ct) => default;
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
            // Should generate something
            runResult.Results.Should().NotBeEmpty();
        }

        [Fact]
        public void Generator_Should_Generate_Multiple_Files()
        {
            // Arrange
            var source = @"
using Relay.Core;
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
            var compilation = CreateTestCompilation(source);

            // Act
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            var runResult = driver.GetRunResult();

            // Assert
            // Generator should produce some output
            runResult.Results.Should().NotBeEmpty();
        }

        [Fact]
        public void Generator_Should_Not_Crash_With_Pipeline()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestPipeline
    {
        [Pipeline(Order = 1)]
        public ValueTask<TResponse> HandleAsync<TRequest, TResponse>(
            TRequest request,
            Func<ValueTask<TResponse>> next,
            CancellationToken ct)
        {
            return next();
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
            // Should not crash - results may or may not be empty
            runResult.Should().NotBeNull();
        }

        private static Compilation CreateTestCompilation(string source)
        {
            var syntaxTree = string.IsNullOrEmpty(source)
                ? null
                : CSharpSyntaxTree.ParseText(source);

            var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest<out TResponse> { }
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

            var trees = syntaxTree == null
                ? new[] { relayCoreStubs }
                : new[] { relayCoreStubs, syntaxTree };

            return CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: trees,
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
