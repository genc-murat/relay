using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;

namespace Relay.SourceGenerator.Tests;

public class HandlerConfigurationValidationTests
{
    private readonly Mock<IDiagnosticReporter> _mockReporter;
    private readonly ConfigurationValidator _validator;

    public HandlerConfigurationValidationTests()
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

    [Fact]
    public void ValidateHandlerConfigurations_InvalidHandlerReturnType_ShouldReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle] public int Handle(TestRequest request) => 0;
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

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_202")), Times.Once);
    }

    [Fact]
    public void ValidateHandlerConfigurations_HandlerMissingRequestParameter_ShouldReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle] public string Handle() => string.Empty;
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

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_205")), Times.Once);
    }

    [Fact]
    public void ValidateHandlerConfigurations_InvalidStreamHandlerReturnType_ShouldReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestStreamRequest : IStreamRequest<string> { }
                public class TestStreamHandler
                {
                    [Handle] public string HandleStream(TestStreamRequest request) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestStreamRequest");
        var responseType = GetTypeSymbol(compilation, "string");
        var handlerType = GetTypeSymbol(compilation, "TestStreamHandler");
        var method = handlerType.GetMembers("HandleStream").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = responseType,
                Method = method,
                Name = null,
                Priority = 0,
                Kind = HandlerKind.Stream,
                Location = Location.None
            }
        };

        // Act
        _validator.ValidateHandlerConfigurations(handlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_203")), Times.Once);
    }

    [Fact]
    public void ValidateHandlerConfigurations_HandlerInvalidRequestParameter_ShouldReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class WrongRequest { }
                public class TestHandler
                {
                    [Handle] public string Handle(WrongRequest request) => string.Empty;
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

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_206")), Times.Once);
    }

    [Fact]
    public void ValidateHandlerConfigurations_HandlerMissingCancellationToken_ShouldReportWarning()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle] public string Handle(TestRequest request) => string.Empty;
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

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_207")), Times.Once);
    }

    [Fact]
    public void ValidateHandlerConfigurations_ValidHandlerWithCancellationToken_ShouldNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle] public string Handle(TestRequest request, System.Threading.CancellationToken cancellationToken) => string.Empty;
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

    [Fact]
    public void ValidateHandlerConfigurations_ValidStreamHandler_ShouldNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestStreamRequest : IStreamRequest<string> { }
                public class TestStreamHandler
                {
                    [Handle] public System.Collections.Generic.IAsyncEnumerable<string> HandleStream(TestStreamRequest request) => null;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestStreamRequest");
        var responseType = GetTypeSymbol(compilation, "string");
        var handlerType = GetTypeSymbol(compilation, "TestStreamHandler");
        var method = handlerType.GetMembers("HandleStream").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = responseType,
                Method = method,
                Name = null,
                Priority = 0,
                Kind = HandlerKind.Stream,
                Location = Location.None
            }
        };

        // Act
        _validator.ValidateHandlerConfigurations(handlers);

        // Assert - No error diagnostics should be reported
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Severity == DiagnosticSeverity.Error)), Times.Never);
    }

    [Fact]
    public void ValidateHandlerConfigurations_MultipleValidationErrors_ShouldReportAll()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle] public string Handle(TestRequest request) => string.Empty;
                    [Handle] public string Handle2() => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var responseType = GetTypeSymbol(compilation, "string");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method1 = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();
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
            d.Id == "RELAY_GEN_003")), Times.Exactly(2)); // Duplicate handlers
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_205")), Times.Once); // Missing request parameter
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_207")), Times.Exactly(2)); // Missing CancellationToken (both handlers)
    }

    [Fact]
    public void ValidateHandlerConfigurations_WithMixedNamedAndUnnamedHandlers_NoDuplicatesReported()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle] public string HandleUnnamed(TestRequest request) => string.Empty;
                    [Handle(Name = ""Named1"")] public string HandleNamed1(TestRequest request) => string.Empty;
                    [Handle(Name = ""Named2"")] public string HandleNamed2(TestRequest request) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var responseType = GetTypeSymbol(compilation, "string");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var methodUnnamed = handlerType.GetMembers("HandleUnnamed").OfType<IMethodSymbol>().First();
        var methodNamed1 = handlerType.GetMembers("HandleNamed1").OfType<IMethodSymbol>().First();
        var methodNamed2 = handlerType.GetMembers("HandleNamed2").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = responseType,
                Method = methodUnnamed,
                Name = null,
                Priority = 0,
                Kind = HandlerKind.Request,
                Location = Location.None
            },
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = responseType,
                Method = methodNamed1,
                Name = "Named1",
                Priority = 0,
                Kind = HandlerKind.Request,
                Location = Location.None
            },
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = responseType,
                Method = methodNamed2,
                Name = "Named2",
                Priority = 0,
                Kind = HandlerKind.Request,
                Location = Location.None
            }
        };

        // Act
        _validator.ValidateHandlerConfigurations(handlers);

        // Assert - No duplicate diagnostics should be reported
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor.Id == "RELAY_GEN_003" || d.Descriptor.Id == "RELAY_GEN_005")), Times.Never);
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