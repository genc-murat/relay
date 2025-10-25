using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for ConfigurationValidator.ValidateAttributeParameterConflicts method.
/// Validates that attribute parameters are properly validated for conflicts and invalid values.
/// </summary>
public class AttributeParameterConflictValidationTests
{
    private readonly Mock<IDiagnosticReporter> _mockReporter;
    private readonly ConfigurationValidator _validator;

    public AttributeParameterConflictValidationTests()
    {
        _mockReporter = new Mock<IDiagnosticReporter>();
        _validator = new ConfigurationValidator(_mockReporter.Object);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithValidPriority_DoesNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Priority = 10)]
                    public string Handle(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = null,
                Priority = 10,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert - No diagnostics should be reported for valid priority
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithValidName_DoesNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Name = ""CustomHandler"")]
                    public string Handle(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = "CustomHandler",
                Priority = 0,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert - No diagnostics should be reported for valid name
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithMultipleValidHandlers_DoesNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Name = ""Handler1"", Priority = 1)]
                    public string Handle1(TestRequest request, CancellationToken ct) => string.Empty;

                    [Handle(Name = ""Handler2"", Priority = 2)]
                    public string Handle2(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method1 = handlerType.GetMembers("Handle1").OfType<IMethodSymbol>().First();
        var method2 = handlerType.GetMembers("Handle2").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method1,
                Name = "Handler1",
                Priority = 1,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            },
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method2,
                Name = "Handler2",
                Priority = 2,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert - No diagnostics should be reported for valid distinct handlers
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithZeroPriority_DoesNotReportError()
    {
        // Arrange - Priority 0 is a valid default value
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Priority = 0)]
                    public string Handle(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = null,
                Priority = 0,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithNegativePriority_DoesNotReportError()
    {
        // Arrange - Negative priority is valid for low-priority handlers
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Priority = -100)]
                    public string Handle(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = null,
                Priority = -100,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithMaxIntPriority_DoesNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Priority = 2147483647)]
                    public string Handle(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = null,
                Priority = int.MaxValue,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithMinIntPriority_DoesNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Priority = -2147483648)]
                    public string Handle(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = null,
                Priority = int.MinValue,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithEmptyHandlerList_DoesNotReportError()
    {
        // Arrange
        var handlers = new List<HandlerRegistration>();

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert - No error should be reported for empty handlers list
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithHandlerHavingNullAttribute_DoesNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle]
                    public string Handle(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = null,
                Priority = 0,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null // Explicitly null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithNameAndPriority_DoesNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Name = ""ImportantHandler"", Priority = 100)]
                    public string Handle(TestRequest request, CancellationToken ct) => string.Empty;
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

        var handlers = new[]
        {
            new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = "ImportantHandler",
                Priority = 100,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            }
        };

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    [Fact]
    public void ValidateAttributeParameterConflicts_WithManyHandlers_ValidatesAll()
    {
        // Arrange - Create 10 handlers with different priorities and names
        var compilation = CreateCompilation(@"
                public class TestRequest : IRequest<string> { }
                public class TestHandler
                {
                    [Handle(Name = ""H1"", Priority = 1)] public string H1(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H2"", Priority = 2)] public string H2(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H3"", Priority = 3)] public string H3(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H4"", Priority = 4)] public string H4(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H5"", Priority = 5)] public string H5(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H6"", Priority = 6)] public string H6(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H7"", Priority = 7)] public string H7(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H8"", Priority = 8)] public string H8(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H9"", Priority = 9)] public string H9(TestRequest r, CancellationToken ct) => """";
                    [Handle(Name = ""H10"", Priority = 10)] public string H10(TestRequest r, CancellationToken ct) => """";
                }
            ");

        var requestType = GetTypeSymbol(compilation, "TestRequest");
        var handlerType = GetTypeSymbol(compilation, "TestHandler");

        var handlers = new List<HandlerRegistration>();
        for (int i = 1; i <= 10; i++)
        {
            var method = handlerType.GetMembers($"H{i}").OfType<IMethodSymbol>().First();
            handlers.Add(new HandlerRegistration
            {
                RequestType = requestType,
                ResponseType = null,
                Method = method,
                Name = $"H{i}",
                Priority = i,
                Kind = HandlerKind.Request,
                Location = Location.None,
                Attribute = null
            });
        }

        // Act
        _validator.ValidateAttributeParameterConflicts(handlers);

        // Assert - All handlers are valid, no errors expected
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
