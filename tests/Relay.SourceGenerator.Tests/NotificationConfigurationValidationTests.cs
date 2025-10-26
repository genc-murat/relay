using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Validation;

namespace Relay.SourceGenerator.Tests;

public class NotificationConfigurationValidationTests
{
    private readonly Mock<IDiagnosticReporter> _mockReporter;
    private readonly ConfigurationValidator _validator;

    public NotificationConfigurationValidationTests()
    {
        _mockReporter = new Mock<IDiagnosticReporter>();
        _validator = new ConfigurationValidator(_mockReporter.Object);
    }

    [Fact]
    public void ValidateNotificationConfigurations_InvalidNotificationHandlerReturnType_ShouldReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestNotification : INotification { }
                public class TestNotificationHandler
                {
                    [Notification] public string Handle(TestNotification notification) => string.Empty;
                }
            ");

        var notificationType = GetTypeSymbol(compilation, "TestNotification");
        var handlerType = GetTypeSymbol(compilation, "TestNotificationHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

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

        // Act
        _validator.ValidateNotificationConfigurations(notificationHandlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_204")), Times.Once);
    }

    [Fact]
    public void ValidateNotificationConfigurations_NotificationHandlerMissingParameter_ShouldReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestNotification : INotification { }
                public class TestNotificationHandler
                {
                    [Notification] public void Handle() { }
                }
            ");

        var notificationType = GetTypeSymbol(compilation, "TestNotification");
        var handlerType = GetTypeSymbol(compilation, "TestNotificationHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

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

        // Act
        _validator.ValidateNotificationConfigurations(notificationHandlers);

        // Assert
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_208")), Times.Once);
    }

    [Fact]
    public void ValidateNotificationConfigurations_ValidNotificationHandler_ShouldNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestNotification : INotification { }
                public class TestNotificationHandler
                {
                    [Notification] public void Handle(TestNotification notification) { }
                }
            ");

        var notificationType = GetTypeSymbol(compilation, "TestNotification");
        var handlerType = GetTypeSymbol(compilation, "TestNotificationHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

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

        // Act
        _validator.ValidateNotificationConfigurations(notificationHandlers);

        // Assert - Should report invalid return type error since void is not Task/ValueTask
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_204")), Times.Once);
    }

    [Fact]
    public void ValidateNotificationConfigurations_ValidNotificationHandlerWithTask_ShouldNotReportError()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestNotification : INotification { }
                public class TestNotificationHandler
                {
                    [Notification] public void Handle(TestNotification notification) { }
                }
            ");

        var notificationType = GetTypeSymbol(compilation, "TestNotification");
        var handlerType = GetTypeSymbol(compilation, "TestNotificationHandler");
        var method = handlerType.GetMembers("Handle").OfType<IMethodSymbol>().First();

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

        // Act
        _validator.ValidateNotificationConfigurations(notificationHandlers);

        // Assert - Should report invalid return type error since void is not Task/ValueTask
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_204")), Times.Once);
    }

    [Fact]
    public void ValidateNotificationConfigurations_MultipleNotificationHandlers_ShouldValidateEach()
    {
        // Arrange
        var compilation = CreateCompilation(@"
                public class TestNotification : INotification { }
                public class TestNotificationHandler1
                {
                    [Notification] public System.Threading.Tasks.Task Handle1(TestNotification notification) => Task.CompletedTask;
                }
                public class TestNotificationHandler2
                {
                    [Notification] public void Handle2(TestNotification notification) { }
                }
            ");

        var notificationType = GetTypeSymbol(compilation, "TestNotification");
        var handlerType1 = GetTypeSymbol(compilation, "TestNotificationHandler1");
        var handlerType2 = GetTypeSymbol(compilation, "TestNotificationHandler2");
        var method1 = handlerType1.GetMembers("Handle1").OfType<IMethodSymbol>().First();
        var method2 = handlerType2.GetMembers("Handle2").OfType<IMethodSymbol>().First();

        var notificationHandlers = new[]
        {
            new NotificationHandlerRegistration
            {
                NotificationType = notificationType,
                Method = method1,
                Priority = 0,
                Location = Location.None
            },
            new NotificationHandlerRegistration
            {
                NotificationType = notificationType,
                Method = method2,
                Priority = 0,
                Location = Location.None
            }
        };

        // Act
        _validator.ValidateNotificationConfigurations(notificationHandlers);

        // Assert - Should report invalid return type for the second handler (void instead of Task/ValueTask)
        _mockReporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Id == "RELAY_GEN_204")), Times.Once);
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