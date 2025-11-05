using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class ServiceRegistrationValidationTests
{
    [Fact]
    public void ValidateServices_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServices(null!));
    }

    [Fact]
    public void ValidateServices_WithValidServices_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        ServiceRegistrationHelper.ValidateServices(services);
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndConfiguration<string>(null!, _ => { }));
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndConfiguration<string>(services, null!));
    }

    [Fact]
    public void ValidateServicesAndConfiguration_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        ServiceRegistrationHelper.ValidateServicesAndConfiguration<string>(services, _ => { });
    }

    [Fact]
    public void ValidateServicesAndFactory_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndFactory<string>(null!, _ => ""));
    }

    [Fact]
    public void ValidateServicesAndFactory_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ValidateServicesAndFactory<string>(services, null!));
    }

    [Fact]
    public void ValidateServicesAndFactory_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        ServiceRegistrationHelper.ValidateServicesAndFactory<string>(services, _ => "");
    }

    // Test interfaces and classes
    public interface ITestService { }
    public class TestService : ITestService { }
    public interface ITestService2 { }
    public class TestService2 : ITestService2 { }

    public class TestOptions
    {
        public string? Value { get; set; }
    }
}
