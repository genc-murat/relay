using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Extensions;

public class ServiceRegistrationOptionsTests
{
    [Fact]
    public void ConfigureOptions_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ConfigureOptions<TestOptions>(null!, _ => { }));
    }

    [Fact]
    public void ConfigureOptions_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ConfigureOptions<TestOptions>(services, null!));
    }

    [Fact]
    public void ConfigureOptions_WithValidParameters_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptions<TestOptions>(services, options => options.Value = "test");

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.Equal("test", options.Value.Value);
    }

    [Fact]
    public void ConfigureOptionsWithDefault_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationHelper.ConfigureOptionsWithDefault<TestOptions>(null!));
    }

    [Fact]
    public void ConfigureOptionsWithDefault_WithNullConfigure_ConfiguresDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptionsWithDefault<TestOptions>(services, null);

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void ConfigureOptionsWithDefault_WithConfigure_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = ServiceRegistrationHelper.ConfigureOptionsWithDefault<TestOptions>(services, options => options.Value = "configured");

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TestOptions>>();
        Assert.Equal("configured", options.Value.Value);
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