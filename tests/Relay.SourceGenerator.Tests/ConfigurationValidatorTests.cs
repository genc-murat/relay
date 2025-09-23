using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Relay.SourceGenerator;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class ConfigurationValidatorTests
    {
        private readonly Mock<IDiagnosticReporter> _mockReporter;
        private readonly ConfigurationValidator _validator;

        public ConfigurationValidatorTests()
        {
            _mockReporter = new Mock<IDiagnosticReporter>();
            _validator = new ConfigurationValidator(_mockReporter.Object);
        }

        [Fact]
        public void ValidateHandlerConfigurations_WithDuplicateUnnamedHandlers_ReportsDuplicateHandler()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler 
                {
                    [Handle] public string Handle1(TestRequest request) => string.Empty;
                    [Handle] public string Handle2(TestRequest request) => string.Empty;
                }
            ");

            var requestType = GetTypeSymbol(compilation, "TestRequest");
            var responseType = GetTypeSymbol(compilation, "string");
            var handlerType = GetTypeSymbol(compilation, "TestHandler");
            var method1 = handlerType.GetMembers("Handle1").OfType<IMethodSymbol>().First();
            var method2 = handlerType.GetMembers("Handle2").OfType<IMethodSymbol>().First();

            var handlers = new[]
            {
                new HandlerRegistration
                {
                    RequestType = requestType,
                    ResponseType = responseType,
                    Method = method1,
                    Name = null,
                    Priority = 0,
                    Kind = HandlerKind.Request,
                    Location = Location.None
                },
                new HandlerRegistration
                {
                    RequestType = requestType,
                    ResponseType = responseType,
                    Method = method2,
                    Name = null,
                    Priority = 0,
                    Kind = HandlerKind.Request,
                    Location = Location.None
                }
            };

            // Act
            _validator.ValidateHandlerConfigurations(handlers);

            // Assert
            _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
                d.Descriptor.Id == "RELAY_GEN_003")), Times.Exactly(2));
        }

        [Fact]
        public void ValidateHandlerConfigurations_WithDuplicateNamedHandlers_ReportsDuplicateNamedHandler()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler 
                {
                    [Handle(Name = ""SameName"")] public string Handle1(TestRequest request) => string.Empty;
                    [Handle(Name = ""SameName"")] public string Handle2(TestRequest request) => string.Empty;
                }
            ");

            var requestType = GetTypeSymbol(compilation, "TestRequest");
            var responseType = GetTypeSymbol(compilation, "string");
            var handlerType = GetTypeSymbol(compilation, "TestHandler");
            var method1 = handlerType.GetMembers("Handle1").OfType<IMethodSymbol>().First();
            var method2 = handlerType.GetMembers("Handle2").OfType<IMethodSymbol>().First();

            var handlers = new[]
            {
                new HandlerRegistration
                {
                    RequestType = requestType,
                    ResponseType = responseType,
                    Method = method1,
                    Name = "SameName",
                    Priority = 0,
                    Kind = HandlerKind.Request,
                    Location = Location.None
                },
                new HandlerRegistration
                {
                    RequestType = requestType,
                    ResponseType = responseType,
                    Method = method2,
                    Name = "SameName",
                    Priority = 0,
                    Kind = HandlerKind.Request,
                    Location = Location.None
                }
            };

            // Act
            _validator.ValidateHandlerConfigurations(handlers);

            // Assert
            _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
                d.Descriptor.Id == "RELAY_GEN_005")), Times.Exactly(2));
        }

        [Fact]
        public void ValidatePipelineConfigurations_WithDuplicateOrders_ReportsDuplicatePipelineOrder()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                public class TestPipeline 
                {
                    [Pipeline(Order = 1)] public void Pipeline1() { }
                    [Pipeline(Order = 1)] public void Pipeline2() { }
                }
            ");

            var pipelineType = GetTypeSymbol(compilation, "TestPipeline");
            var method1 = pipelineType.GetMembers("Pipeline1").OfType<IMethodSymbol>().First();
            var method2 = pipelineType.GetMembers("Pipeline2").OfType<IMethodSymbol>().First();

            var pipelines = new[]
            {
                new PipelineRegistration
                {
                    PipelineType = pipelineType,
                    Method = method1,
                    Order = 1,
                    Scope = PipelineScope.All,
                    Location = Location.None
                },
                new PipelineRegistration
                {
                    PipelineType = pipelineType,
                    Method = method2,
                    Order = 1,
                    Scope = PipelineScope.All,
                    Location = Location.None
                }
            };

            // Act
            _validator.ValidatePipelineConfigurations(pipelines);

            // Assert
            _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
                d.Descriptor.Id == "RELAY_GEN_201")), Times.Exactly(2));
        }

        [Fact]
        public void ValidateConfigurationCompleteness_WithNoHandlers_ReportsNoHandlersFound()
        {
            // Arrange
            var emptyHandlers = Enumerable.Empty<HandlerRegistration>();
            var emptyNotificationHandlers = Enumerable.Empty<NotificationHandlerRegistration>();
            var emptyPipelines = Enumerable.Empty<PipelineRegistration>();

            // Act
            _validator.ValidateConfigurationCompleteness(emptyHandlers, emptyNotificationHandlers, emptyPipelines);

            // Assert
            _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
                d.Descriptor.Id == "RELAY_GEN_210")), Times.Once);
        }

        [Fact]
        public void ValidateHandlerConfigurations_WithValidConfiguration_DoesNotReportErrors()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler 
                {
                    [Handle] public string Handle(TestRequest request, CancellationToken cancellationToken) => string.Empty;
                }
            ");

            var requestType = GetTypeSymbol(compilation, "TestRequest");
            var responseType = GetTypeSymbol(compilation, "string");
            var handlerType = GetTypeSymbol(compilation, "TestHandler");
            var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

            var handlers = new[]
            {
                new HandlerRegistration
                {
                    RequestType = requestType,
                    ResponseType = responseType,
                    Method = method,
                    Name = null,
                    Priority = 0,
                    Kind = HandlerKind.Request,
                    Location = Location.None
                }
            };

            // Act
            _validator.ValidateHandlerConfigurations(handlers);

            // Assert - No error diagnostics should be reported
            _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
                d.Severity == DiagnosticSeverity.Error)), Times.Never);
        }

        private static Compilation CreateCompilation(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText($@"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Relay.Core
{{
    public interface IRequest<out TResponse> {{ }}
    public class HandleAttribute : Attribute 
    {{ 
        public string? Name {{ get; set; }}
        public int Priority {{ get; set; }}
    }}
    public class PipelineAttribute : Attribute 
    {{ 
        public int Order {{ get; set; }}
    }}
}}

{source}
");

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location)
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static INamedTypeSymbol GetTypeSymbol(Compilation compilation, string typeName)
        {
            // Handle C# builtin aliases
            switch (typeName)
            {
                case "string":
                    return (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_String);
                case "int":
                    return (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32);
                case "bool":
                    return (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Boolean);
                case "void":
                    return (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Void);
            }

            return compilation.GetTypeByMetadataName(typeName) ??
                   compilation.GetSymbolsWithName(typeName).OfType<INamedTypeSymbol>().First();
        }
    }
}