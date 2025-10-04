using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using FluentAssertions;
using System.Linq;

namespace Relay.SourceGenerator.Tests;

public class NotificationHandlerRegistrationTests
{
    [Fact]
    public void NotificationHandlerRegistration_ShouldSetNotificationType()
    {
        // Arrange
        var compilation = CreateTestCompilation();
        var notificationType = compilation.GetTypeByMetadataName("System.Object");

        // Act
        var registration = new NotificationHandlerRegistration
        {
            NotificationType = notificationType!
        };

        // Assert
        registration.NotificationType.Should().NotBeNull();
        registration.NotificationType.Should().Be(notificationType);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSetMethod()
    {
        // Arrange
        var compilation = CreateTestCompilation();
        var testClass = compilation.GetTypeByMetadataName("System.Object");
        var method = testClass?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();

        // Act
        var registration = new NotificationHandlerRegistration
        {
            Method = method!
        };

        // Assert
        registration.Method.Should().NotBeNull();
        registration.Method.Should().Be(method);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSetPriority()
    {
        // Arrange & Act
        var registration = new NotificationHandlerRegistration
        {
            Priority = 100
        };

        // Assert
        registration.Priority.Should().Be(100);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSetLocation()
    {
        // Arrange
        var location = Location.None;

        // Act
        var registration = new NotificationHandlerRegistration
        {
            Location = location
        };

        // Assert
        registration.Location.Should().Be(location);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSetAttribute()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
using System;

namespace TestApp
{
    [Obsolete]
    public class TestClass { }
}");

        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");
        var attribute = testClass?.GetAttributes().FirstOrDefault();

        // Act
        var registration = new NotificationHandlerRegistration
        {
            Attribute = attribute
        };

        // Assert
        registration.Attribute.Should().Be(attribute);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldAllowNullAttribute()
    {
        // Arrange & Act
        var registration = new NotificationHandlerRegistration
        {
            Attribute = null
        };

        // Assert
        registration.Attribute.Should().BeNull();
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSetAllProperties()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
using System;

namespace TestApp
{
    public interface INotification { }
    public class TestNotification : INotification { }

    public class Handler
    {
        [Obsolete]
        public void HandleAsync(TestNotification notification) { }
    }
}");

        var notificationType = compilation.GetTypeByMetadataName("TestApp.TestNotification");
        var handlerType = compilation.GetTypeByMetadataName("TestApp.Handler");
        var method = handlerType?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();
        var attribute = method?.GetAttributes().FirstOrDefault();
        var location = method?.Locations.FirstOrDefault() ?? Location.None;

        // Act
        var registration = new NotificationHandlerRegistration
        {
            NotificationType = notificationType!,
            Method = method!,
            Priority = 50,
            Location = location,
            Attribute = attribute
        };

        // Assert
        registration.NotificationType.Should().Be(notificationType);
        registration.Method.Should().Be(method);
        registration.Priority.Should().Be(50);
        registration.Location.Should().Be(location);
        registration.Attribute.Should().Be(attribute);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSupportNegativePriority()
    {
        // Arrange & Act
        var registration = new NotificationHandlerRegistration
        {
            Priority = -10
        };

        // Assert
        registration.Priority.Should().Be(-10);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSupportZeroPriority()
    {
        // Arrange & Act
        var registration = new NotificationHandlerRegistration
        {
            Priority = 0
        };

        // Assert
        registration.Priority.Should().Be(0);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSupportHighPriority()
    {
        // Arrange & Act
        var registration = new NotificationHandlerRegistration
        {
            Priority = int.MaxValue
        };

        // Assert
        registration.Priority.Should().Be(int.MaxValue);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldSupportLowPriority()
    {
        // Arrange & Act
        var registration = new NotificationHandlerRegistration
        {
            Priority = int.MinValue
        };

        // Assert
        registration.Priority.Should().Be(int.MinValue);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldStoreNotificationTypeWithGenericParameters()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
using System.Collections.Generic;

namespace TestApp
{
    public interface INotification { }
    public class TestNotification<T> : INotification { }
}");

        var notificationType = compilation.GetTypeByMetadataName("TestApp.TestNotification`1");

        // Act
        var registration = new NotificationHandlerRegistration
        {
            NotificationType = notificationType!
        };

        // Assert
        registration.NotificationType.Should().NotBeNull();
        registration.NotificationType.ToDisplayString().Should().Contain("TestNotification<T>");
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldStoreMethodWithParameters()
    {
        // Arrange
        var compilation = CreateTestCompilation(@"
using System.Threading;

namespace TestApp
{
    public interface INotification { }
    public class TestNotification : INotification { }

    public class Handler
    {
        public void HandleAsync(TestNotification notification, CancellationToken cancellationToken) { }
    }
}");

        var handlerType = compilation.GetTypeByMetadataName("TestApp.Handler");
        var method = handlerType?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();

        // Act
        var registration = new NotificationHandlerRegistration
        {
            Method = method!
        };

        // Assert
        registration.Method.Should().NotBeNull();
        registration.Method.Parameters.Should().HaveCount(2);
    }

    [Fact]
    public void NotificationHandlerRegistration_ShouldStoreLocationWithSourceInfo()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class Handler
    {
        public void HandleAsync() { }
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var handlerType = compilation.GetTypeByMetadataName("TestApp.Handler");
        var method = handlerType?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();
        var location = method?.Locations.FirstOrDefault() ?? Location.None;

        // Act
        var registration = new NotificationHandlerRegistration
        {
            Location = location
        };

        // Assert
        registration.Location.Should().NotBe(Location.None);
        registration.Location.SourceTree.Should().NotBeNull();
    }

    private Compilation CreateTestCompilation(string? additionalSource = null)
    {
        var source = additionalSource ?? "namespace Test { }";
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
