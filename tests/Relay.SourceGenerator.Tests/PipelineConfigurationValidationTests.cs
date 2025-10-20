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
    public class PipelineConfigurationValidationTests
    {
        private readonly Mock<IDiagnosticReporter> _mockReporter;
        private readonly ConfigurationValidator _validator;

        public PipelineConfigurationValidationTests()
        {
            _mockReporter = new Mock<IDiagnosticReporter>();
            _validator = new ConfigurationValidator(_mockReporter.Object);
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
        public void ValidatePipelineConfigurations_WithValidConfigurations_ReportsNoDiagnostics()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                public class TestPipeline
                {
                    [Pipeline(Order = 1)] public void Pipeline1() { }
                    [Pipeline(Order = 2)] public void Pipeline2() { }
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
                    Order = 2,
                    Scope = PipelineScope.All,
                    Location = Location.None
                }
            };

            // Act
            _validator.ValidatePipelineConfigurations(pipelines);

            // Assert
            _mockReporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
        }

        [Fact]
        public void ValidatePipelineConfigurations_WithDifferentScopes_ValidatesSeparately()
        {
            // Arrange
            var compilation = CreateCompilation(@"
                public class TestPipeline
                {
                    [Pipeline(Order = 1, Scope = PipelineScope.Requests)] public void Pipeline1() { }
                    [Pipeline(Order = 1, Scope = PipelineScope.Notifications)] public void Pipeline2() { }
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
                    Scope = PipelineScope.Requests,
                    Location = Location.None
                },
                new PipelineRegistration
                {
                    PipelineType = pipelineType,
                    Method = method2,
                    Order = 1,
                    Scope = PipelineScope.Notifications,
                    Location = Location.None
                }
            };

            // Act
            _validator.ValidatePipelineConfigurations(pipelines);

            // Assert - Should not report duplicates since they're in different scopes
            _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
                d.Descriptor.Id == "RELAY_GEN_201")), Times.Never);
        }

        [Fact]
        public void ValidatePipelineConfigurations_WithNullMethod_DoesNotThrowException()
        {
            // Arrange
            var pipelines = new[]
            {
                new PipelineRegistration
                {
                    PipelineType = null,
                    Method = null, // System modules don't have methods
                    Order = 1,
                    Scope = PipelineScope.All,
                    Location = Location.None
                }
            };

            // Act & Assert - Should not throw exception
            _validator.ValidatePipelineConfigurations(pipelines);
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
}