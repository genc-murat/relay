using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class RelayIncrementalGeneratorMissingReferenceTests
    {
        [Fact]
        public void Generator_Should_Report_MissingRelayCoreReference_When_Using_Relay_Attributes()
        {
            // Arrange - Use Relay attributes without referencing Relay.Core
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestClass
    {
        [Handle]
        public ValueTask<string> HandleRequest(string request, CancellationToken ct)
        {
            return ValueTask.FromResult(request);
        }
    }
}";

            // Create compilation WITHOUT Relay.Core reference
            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { 
                    CSharpSyntaxTree.ParseText(@"
                        [System.AttributeUsage(System.AttributeTargets.Method)]
                        public class HandleAttribute : System.Attribute { }
                    "),
                    CSharpSyntaxTree.ParseText(source) 
                },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new RelayIncrementalGenerator();

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
        public void Generator_Should_Not_Report_MissingRelayCoreReference_When_Not_Using_Relay_Attributes()
        {
            // Arrange - Don't use any Relay attributes
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class RegularClass
    {
        public ValueTask<string> SomeMethod(string param, CancellationToken ct)
        {
            return ValueTask.FromResult(param);
        }
    }
}";

            // Create compilation WITHOUT Relay.Core reference
            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { 
                    CSharpSyntaxTree.ParseText(source) 
                },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new RelayIncrementalGenerator();

            // Act
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            // Assert - No missing reference diagnostic should be reported since no Relay attributes used
            var missingCoreDiagnostic = diagnostics.FirstOrDefault(d => 
                d.Id == "RELAY_GEN_004"); // MissingRelayCoreReference
            
            Assert.Null(missingCoreDiagnostic);
        }

        [Fact]
        public void Generator_Should_Handle_Only_Interfaces_Without_Attributes()
        {
            // Arrange - Define interfaces but don't implement them
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    // This is just a class, not implementing any handler interface
    public class SimpleClass
    {
        public int Value { get; set; }
    }
    
    // This implements a handler interface
    public class HandlerClass : IRequestHandler<TestRequest, string>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""handled"");
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
            var generatedCode = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
            
            // Should only register the HandlerClass, not SimpleClass
            Assert.Contains("HandlerClass", generatedCode);
            Assert.DoesNotContain("SimpleClass", generatedCode);
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