using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Validation;

namespace Relay.SourceGenerator.Tests;

public class ConfigurationCompletenessValidationTests
{
    private readonly Mock<IDiagnosticReporter> _mockReporter;
    private readonly ConfigurationValidator _validator;

    public ConfigurationCompletenessValidationTests()
    {
        _mockReporter = new Mock<IDiagnosticReporter>();
        _validator = new ConfigurationValidator(_mockReporter.Object);
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
    public void ValidateConfigurationCompleteness_WithHandlersAndPipelines_DoesNotReportNoHandlersFound()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle] public string Handle(TestRequest request, CancellationToken cancellationToken) => string.Empty;
                }
                public class TestPipeline
                {
                    [Pipeline(Order = 1)] public void Pipeline1() { }
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var responseType = GetTypeSymbol(compilation, "string");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var pipelineType = GetTypeSymbol(compilation, "TestPipeline");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();
        var pipelineMethod = pipelineType.GetMembers("Pipeline1").OfType<IMethodSymbol>().First();

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

        var notificationHandlers = Enumerable.Empty<NotificationHandlerRegistration>();

        var pipelines = new[]
        {
            new PipelineRegistration
            {
                PipelineType = pipelineType,
                Method = pipelineMethod,
                Order = 1,
                Scope = PipelineScope.All,
                Location = Location.None
            }
        };

        // Act
        _validator.ValidateConfigurationCompleteness(handlers, notificationHandlers, pipelines);

        // Assert - Should not report no handlers found since we have handlers
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_210")), Times.Never);
    }

    [Fact]
    public void ValidateConfigurationCompleteness_WithEmptyCollections_DoesNotThrow()
    {
        // Arrange
        var emptyHandlers = Enumerable.Empty<HandlerRegistration>();
        var emptyNotificationHandlers = Enumerable.Empty<NotificationHandlerRegistration>();
        var emptyPipelines = Enumerable.Empty<PipelineRegistration>();

        // Act & Assert - Should not throw
        _validator.ValidateConfigurationCompleteness(emptyHandlers, emptyNotificationHandlers, emptyPipelines);
    }

    [Fact]
    public void ValidateConfigurationCompleteness_WithOnlyNotificationHandlers_DoesNotReportNoHandlersFound()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestNotification : INotification { }
                public class TestNotificationHandler
                {
                    [Notification] public System.Threading.Tasks.Task Handle(TestNotification notification) => Task.CompletedTask;
                }
            ");

        var notificationType = GetTypeSymbol(compilation, "TestNotification");
        var handlerType = GetTypeSymbol(compilation, "TestNotificationHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = Enumerable.Empty<HandlerRegistration>();

        var notificationHandlers = new[]
        {
            new NotificationHandlerRegistration
            {
                NotificationType = notificationType,
                Method = method,
                Priority = 0,
                Location = Location.None
            }
        };

        var pipelines = Enumerable.Empty<PipelineRegistration>();

        // Act
        _validator.ValidateConfigurationCompleteness(handlers, notificationHandlers, pipelines);

        // Assert - Should not report no handlers found since we have notification handlers
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_210")), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithValidAttribute_DoesNotReportErrors()
    {
        // Arrange
        var requestType = Mock.Of<ITypeSymbol>();
        var responseType = Mock.Of<ITypeSymbol>();
        var method = Mock.Of<IMethodSymbol>();
        var attribute = Mock.Of<AttributeData>();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = responseType,
                Method = method,
                Name = "Test",
                Priority = 1,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = attribute
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert - No diagnostics should be reported
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText($@"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Relay.Core
{{
    public interface IRequest<out TResponse> {{ }}
    public interface IStreamRequest<out TResponse> {{ }}
    public interface INotification {{ }}
    public class HandleAttribute : Attribute
    {{
        public string? Name {{ get; set; }}
        public int Priority {{ get; set; }}
    }}
    public class PipelineAttribute : Attribute
    {{
        public int Order {{ get; set; }}
    }}
    public class NotificationAttribute : Attribute
    {{
        public int Priority {{ get; set; }}
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
        return typeName switch
        {
            "string" => (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_String),
            "int" => (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32),
            "bool" => (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Boolean),
            "void" => (INamedTypeSymbol)compilation.GetSpecialType(SpecialType.System_Void),
            _ => compilation.GetTypeByMetadataName(typeName) ??
                           compilation.GetSymbolsWithName(typeName).OfType<INamedTypeSymbol>().First(),
        };
    }
}