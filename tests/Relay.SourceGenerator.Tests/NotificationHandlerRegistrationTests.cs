using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
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
        Assert.NotNull(registration.NotificationType);
        Assert.Equal(notificationType, registration.NotificationType);
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
        Assert.NotNull(registration.Method);
        Assert.Equal(method, registration.Method);
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
        Assert.Equal(100, registration.Priority);
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
        Assert.Equal(location, registration.Location);
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
        Assert.Equal(attribute, registration.Attribute);
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
        Assert.Null(registration.Attribute);
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
        Assert.Equal(notificationType, registration.NotificationType);
        Assert.Equal(method, registration.Method);
        Assert.Equal(50, registration.Priority);
        Assert.Equal(location, registration.Location);
        Assert.Equal(attribute, registration.Attribute);
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
        Assert.Equal(-10, registration.Priority);
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
        Assert.Equal(0, registration.Priority);
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
        Assert.Equal(int.MaxValue, registration.Priority);
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
        Assert.Equal(int.MinValue, registration.Priority);
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
        Assert.NotNull(registration.NotificationType);
        Assert.Contains("TestNotification<T>", registration.NotificationType.ToDisplayString());
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
        Assert.NotNull(registration.Method);
        Assert.Equal(2, registration.Method.Parameters.Length);
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
        Assert.NotEqual(Location.None, registration.Location);
        Assert.NotNull(registration.Location.SourceTree);
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
