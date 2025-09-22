using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Relay.SourceGenerator;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class HandlerDiscoveryTests
    {
        [Fact]
        public void DiscoverHandlers_Should_Find_Valid_Request_Handler()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public Task<string> HandleTest(string request)
        {
            return Task.FromResult(request);
        }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(1);
            var handler = result.Handlers.First();
            handler.MethodSymbol.Name.Should().Be("HandleTest");
            handler.Attributes.Should().HaveCount(1);
            handler.Attributes.First().Type.Should().Be(RelayAttributeType.Handle);
        }

        [Fact]
        public void DiscoverHandlers_Should_Find_Valid_Notification_Handler()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Notification]
        public Task HandleNotification(string notification)
        {
            return Task.CompletedTask;
        }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(1);
            var handler = result.Handlers.First();
            handler.MethodSymbol.Name.Should().Be("HandleNotification");
            handler.Attributes.Should().HaveCount(1);
            handler.Attributes.First().Type.Should().Be(RelayAttributeType.Notification);
        }

        [Fact]
        public void DiscoverHandlers_Should_Find_Valid_Pipeline_Handler()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Pipeline]
        public Task<T> HandlePipeline<T>(T request, Func<Task<T>> next)
        {
            return next();
        }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(1);
            var handler = result.Handlers.First();
            handler.MethodSymbol.Name.Should().Be("HandlePipeline");
            handler.Attributes.Should().HaveCount(1);
            handler.Attributes.First().Type.Should().Be(RelayAttributeType.Pipeline);
        }

        [Fact]
        public void DiscoverHandlers_Should_Find_Valid_Endpoint_Handler()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [ExposeAsEndpoint]
        public Task<string> HandleEndpoint(string request)
        {
            return Task.FromResult(request);
        }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(1);
            var handler = result.Handlers.First();
            handler.MethodSymbol.Name.Should().Be("HandleEndpoint");
            handler.Attributes.Should().HaveCount(1);
            handler.Attributes.First().Type.Should().Be(RelayAttributeType.ExposeAsEndpoint);
        }

        [Fact]
        public void DiscoverHandlers_Should_Reject_Request_Handler_With_No_Parameters()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest()
        {
            return ""test"";
        }
    }
}";

            // Act
            var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

            // Assert
            result.Handlers.Should().BeEmpty();
            diagnostics.Should().Contain(d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("exactly one parameter"));
        }

        [Fact]
        public void DiscoverHandlers_Should_Reject_Request_Handler_With_Multiple_Parameters()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest(string request, int extra)
        {
            return request;
        }
    }
}";

            // Act
            var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

            // Assert
            result.Handlers.Should().BeEmpty();
            diagnostics.Should().Contain(d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("exactly one parameter"));
        }

        [Fact]
        public void DiscoverHandlers_Should_Reject_Notification_Handler_With_Non_Void_Return()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Notification]
        public string HandleNotification(string notification)
        {
            return ""result"";
        }
    }
}";

            // Act
            var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

            // Assert
            result.Handlers.Should().BeEmpty();
            diagnostics.Should().Contain(d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("Task, ValueTask, or void"));
        }

        [Fact]
        public void DiscoverHandlers_Should_Accept_Notification_Handler_With_Void_Return()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Notification]
        public void HandleNotification(string notification)
        {
        }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(1);
        }

        [Fact]
        public void DiscoverHandlers_Should_Accept_ValueTask_Return_Types()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public ValueTask<string> HandleRequest(string request)
        {
            return ValueTask.FromResult(request);
        }

        [Notification]
        public ValueTask HandleNotification(string notification)
        {
            return ValueTask.CompletedTask;
        }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(2);
        }

        [Fact]
        public void DiscoverHandlers_Should_Accept_Streaming_Handler_With_IAsyncEnumerable()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System.Collections.Generic;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public IAsyncEnumerable<string> HandleStream(string request)
        {
            return GetStringsAsync();
        }

        private async IAsyncEnumerable<string> GetStringsAsync()
        {
            yield return ""test"";
        }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(1);
        }

        [Fact]
        public void DiscoverHandlers_Should_Reject_Private_Handler()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        private string HandleTest(string request)
        {
            return request;
        }
    }
}";

            // Act
            var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

            // Assert
            result.Handlers.Should().BeEmpty();
            diagnostics.Should().Contain(d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("public or internal"));
        }

        [Fact]
        public void DiscoverHandlers_Should_Accept_Internal_Handler()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        internal string HandleTest(string request)
        {
            return request;
        }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(1);
        }

        [Fact]
        public void DiscoverHandlers_Should_Reject_Pipeline_Handler_With_One_Parameter()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Pipeline]
        public string HandlePipeline(string request)
        {
            return request;
        }
    }
}";

            // Act
            var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

            // Assert
            result.Handlers.Should().BeEmpty();
            diagnostics.Should().Contain(d => d.Id == "RELAY_GEN_002" && d.GetMessage(null).Contains("at least two parameters"));
        }

        [Fact]
        public void DiscoverHandlers_Should_Find_Multiple_Handlers_In_Same_Class()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleString(string request) => request;

        [Handle]
        public int HandleInt(int request) => request;

        [Notification]
        public void HandleNotification(string notification) { }
    }
}";

            // Act
            var result = RunHandlerDiscovery(source);

            // Assert
            result.Handlers.Should().HaveCount(3);
        }

        [Fact]
        public void DiscoverHandlers_Should_Report_Duplicate_Handlers_For_Same_Request_Type()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleString1(string request) => request;

        [Handle]
        public string HandleString2(string request) => request;
    }
}";

            // Act
            var (result, diagnostics) = RunHandlerDiscoveryWithDiagnostics(source);

            // Assert
            result.Handlers.Should().HaveCount(2); // Both handlers are discovered but marked as duplicates
            diagnostics.Should().Contain(d => d.Id == "RELAY_GEN_005" && d.GetMessage(null).Contains("Multiple handlers"));
        }

        private HandlerDiscoveryResult RunHandlerDiscovery(string source)
        {
            var (result, _) = RunHandlerDiscoveryWithDiagnostics(source);
            return result;
        }

        private (HandlerDiscoveryResult result, Diagnostic[] diagnostics) RunHandlerDiscoveryWithDiagnostics(string source)
        {
            var compilation = CreateTestCompilation(source);
            var context = new RelayCompilationContext(compilation, default);

            // Parse and collect candidate methods
            var syntaxTree = compilation.SyntaxTrees.First();
            var receiver = new RelaySyntaxReceiver();
            foreach (var node in syntaxTree.GetRoot().DescendantNodes())
            {
                receiver.OnVisitSyntaxNode(node);
            }

            // Create a mock diagnostic reporter to collect diagnostics
            var diagnostics = new List<Diagnostic>();
            var mockReporter = new MockDiagnosticReporter(diagnostics);

            // Run discovery
            var discoveryEngine = new HandlerDiscoveryEngine(context);
            var result = discoveryEngine.DiscoverHandlers(receiver.CandidateMethods, mockReporter);

            return (result, diagnostics.ToArray());
        }

        private static CSharpCompilation CreateTestCompilation(string source)
        {
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Relay.Core.IRelay).Assembly.Location),
            };

            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private class MockDiagnosticReporter : IDiagnosticReporter
        {
            private readonly List<Diagnostic> _diagnostics;

            public MockDiagnosticReporter(List<Diagnostic> diagnostics)
            {
                _diagnostics = diagnostics;
            }

            public void ReportDiagnostic(Diagnostic diagnostic)
            {
                _diagnostics.Add(diagnostic);
            }
        }
    }
}