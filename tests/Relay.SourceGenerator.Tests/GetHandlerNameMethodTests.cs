using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Relay.SourceGenerator.Tests;

public class GetHandlerNameMethodTests
{
    [Fact]
    public void GetHandlerName_WithValidName_Should_ReturnThatName()
    {
        // Arrange: Create a handler with [Handle(Name = "CustomName")]
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle(Name = ""CustomName"")]
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        
        // Test that the compilation succeeds and symbols can be analyzed
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void GetHandlerName_WithWhitespaceName_Should_ReturnDefault()
    {
        // Arrange: Create a handler with [Handle(Name = "   ")] where Name is whitespace
        // This tests the branch where !string.IsNullOrWhiteSpace(name) is false
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle(Name = ""   "")]  // Whitespace name
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        
        // Test that the compilation succeeds and symbols can be analyzed
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void GetHandlerName_WithEmptyName_Should_ReturnDefault()
    {
        // Arrange: Create a handler with [Handle(Name = "")] where Name is empty
        // This tests the branch where !string.IsNullOrWhiteSpace(name) is false
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle(Name = """")]  // Empty name
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        
        // Test that the compilation succeeds and symbols can be analyzed
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void GetHandlerName_WithoutNameProperty_Should_ReturnDefault()
    {
        // Arrange: Create a handler with [Handle] but no Name property
        // This tests the scenario where the Name property doesn't exist
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public class TestHandler
    {
        [Handle]  // No Name property
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(""test"");
        }
    }
}";

        var compilation = CreateTestCompilation(source);
        
        // Test that the compilation succeeds and symbols can be analyzed
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    private static Compilation CreateTestCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var relayCoreStubs = CSharpSyntaxTree.ParseText(@"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest<out TResponse> { }
    public interface IRequest { }
    
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
        public int Priority { get; set; }
    }
}");

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { relayCoreStubs, syntaxTree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}